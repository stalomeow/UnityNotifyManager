using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using EasyTypeReload;

[XLua.LuaCallCSharp]
[ReloadOnEnterPlayMode]
public static class NotifyManager
{
    private readonly struct NotifyGroupInfo
    {
        public readonly string Name;
        public readonly NotifyDelegateLink Head;

        public NotifyGroupInfo(string name, NotifyDelegateLink head)
        {
            Name = name;
            Head = head;
        }
    }

    private static readonly Dictionary<Type, NotifyDelegateLink> _notifyLinks = new();
    private static readonly Dictionary<uint, NotifyGroupInfo> _groups = new();
    private static NotifyDelegateLink _freelist = null;

    private static readonly Stack<uint> _recycledGroupIdStack = new();
    private static uint _nextGroupId = 1u; // 0u is invalid!!!

    private static readonly Dictionary<Type, object> _emptyNotifyCache = new();

    public static NotifyExceptionHandler ExceptionHandler;

    public static NotifyGroup NewGroup(string name = "")
    {
        if (!_recycledGroupIdStack.TryPop(out uint id))
        {
            id = _nextGroupId;
            _nextGroupId++;
        }

        _groups.Add(id, new NotifyGroupInfo(name, NewDelegateLink()));
        return new NotifyGroup(id);
    }

    internal static void RemoveGroup(uint groupId)
    {
        NotifyGroupInfo groupInfo = _groups[groupId];
        NotifyDelegateLink curr = groupInfo.Head.GroupOrFreeNext;
        NotifyDelegateLink tail = groupInfo.Head;

        while (curr != null)
        {
            curr.NotifyPrev.NotifyNext = curr.NotifyNext;

            if (curr.NotifyNext != null)
            {
                curr.NotifyNext.NotifyPrev = curr.NotifyPrev;
            }

            curr.ResetMethod();
            curr.ResetNotify();

            tail = curr;
            curr = curr.GroupOrFreeNext;
        }

        tail.GroupOrFreeNext = _freelist;
        _freelist = groupInfo.Head;

        _groups.Remove(groupId);
        _recycledGroupIdStack.Push(groupId);
    }

    internal static string GetGroupName(uint groupId)
    {
        return _groups[groupId].Name;
    }

    internal static void AddListenerToGroup(uint groupId, Type notifyType, NotifyDelegateLink delegateLink)
    {
        NotifyGroupInfo groupInfo = _groups[groupId];
        NotifyDelegateLink notifyListHead = GetOrNewNotifyListHead(notifyType);
        delegateLink.SetGroupIdAndPrependToLinkedList(groupId, groupInfo.Head, notifyListHead);
    }

    private static NotifyDelegateLink GetOrNewNotifyListHead(Type notifyType)
    {
        if (!_notifyLinks.TryGetValue(notifyType, out NotifyDelegateLink head))
        {
            head = NewDelegateLink();
            _notifyLinks.Add(notifyType, head);
        }

        return head;
    }

    internal static NotifyDelegateLink NewDelegateLink()
    {
        if (_freelist == null)
        {
            return new NotifyDelegateLink();
        }

        NotifyDelegateLink result = _freelist;
        _freelist = result.GroupOrFreeNext;
        result.GroupOrFreeNext = null;
        return result;
    }

    public static void Send(object notify)
    {
        Type notifyType = notify.GetType();

        if (!_notifyLinks.TryGetValue(notifyType, out NotifyDelegateLink notifyListHead))
        {
            return;
        }

        NotifyDelegateLink curr = notifyListHead.NotifyNext;

        while (curr != null)
        {
            try
            {
                curr.Invoke(notify);
            }
            catch (Exception e)
            {
                string groupName = GetGroupName(curr.GroupId);

                if (ExceptionHandler != null)
                {
                    ExceptionHandler(notifyType, groupName, e);
                }
                else
                {
                    Debug.LogException(new Exception(
                        $"Error when sending <notify '{notifyType.FullName}'> to <group '{groupName}'>!", e));
                }
            }

            curr = curr.NotifyNext;
        }
    }

    public static void SendEmpty<T>(bool cache = true) where T : class
    {
        SendEmpty(typeof(T), cache);
    }

    public static void SendEmpty(Type notifyType, bool cache = true)
    {
        if (!notifyType.IsClass)
        {
            throw new InvalidOperationException();
        }

        object notify;

        if (cache)
        {
            if (!_emptyNotifyCache.TryGetValue(notifyType, out notify))
            {
                notify = Activator.CreateInstance(notifyType);
                _emptyNotifyCache.Add(notifyType, notify);
            }
        }
        else
        {
            notify = Activator.CreateInstance(notifyType);
        }

        Send(notify);
    }

    [XLua.BlackList]
    public static DeferNotifyScope<T> SendDeferred<T>(out T uninitializedNotify) where T : class, IDisposable, new()
    {
        uninitializedNotify = DeferNotifyScope<T>.CachedNotify;
        return new DeferNotifyScope<T>();
    }

    [XLua.BlackList]
    [ReloadOnEnterPlayMode]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly ref struct DeferNotifyScope<T> where T : class, IDisposable, new()
    {
        public static readonly T CachedNotify = new T();

        public void Dispose()
        {
            Send(CachedNotify);
            CachedNotify.Dispose();
        }
    }
}
