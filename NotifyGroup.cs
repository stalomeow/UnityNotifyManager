using System;
using System.ComponentModel;

[XLua.GCOptimize]
[XLua.LuaCallCSharp]
public struct NotifyGroup
{
    [EditorBrowsable(EditorBrowsableState.Never)] public uint _groupId;

    internal NotifyGroup(uint groupId)
    {
        _groupId = groupId;
    }

    public bool IsValid => _groupId != 0u;

    private void ThrowIfGroupIsInvalid()
    {
        if (!IsValid)
        {
            throw new ObjectDisposedException(nameof(NotifyGroup));
        }
    }

    public void Dispose()
    {
        ThrowIfGroupIsInvalid();
        NotifyManager.RemoveGroup(_groupId);
        _groupId = 0u;
    }

    public string Name
    {
        get
        {
            ThrowIfGroupIsInvalid();
            return NotifyManager.GetGroupName(_groupId);
        }
    }

    public void ListenNoArg(Type notifyType, NotifyZeroArgDelegate method)
    {
        ThrowIfGroupIsInvalid();

        if (!notifyType.IsClass)
        {
            throw new ArgumentException($"{nameof(notifyType)} must be a class.", nameof(notifyType));
        }

        NotifyDelegateLink delegateLink = NotifyManager.NewDelegateLink();
        delegateLink.InitMethod(method);
        NotifyManager.AddListenerToGroup(_groupId, notifyType, delegateLink);
    }

    public void Listen(Type notifyType, NotifyOneArgDelegate method)
    {
        ThrowIfGroupIsInvalid();

        if (!notifyType.IsClass)
        {
            throw new ArgumentException($"{nameof(notifyType)} must be a class.", nameof(notifyType));
        }

        NotifyDelegateLink delegateLink = NotifyManager.NewDelegateLink();
        delegateLink.InitMethod(method);
        NotifyManager.AddListenerToGroup(_groupId, notifyType, delegateLink);
    }

    [XLua.BlackList]
    public unsafe void ListenNoArg<TNotify>(delegate*<void> func)
        where TNotify : class
    {
        ThrowIfGroupIsInvalid();

        NotifyDelegateLink delegateLink = NotifyManager.NewDelegateLink();
        delegateLink.InitMethod(func, hasArg: false);
        NotifyManager.AddListenerToGroup(_groupId, typeof(TNotify), delegateLink);
    }

    [XLua.BlackList]
    public unsafe void Listen<TNotify>(delegate*<TNotify, void> func)
        where TNotify : class
    {
        ThrowIfGroupIsInvalid();

        NotifyDelegateLink delegateLink = NotifyManager.NewDelegateLink();
        delegateLink.InitMethod(func, hasArg: true);
        NotifyManager.AddListenerToGroup(_groupId, typeof(TNotify), delegateLink);
    }

    [XLua.BlackList]
    public unsafe void ListenNoArg<TSelf, TNotify>(TSelf @this, delegate*<TSelf, void> func)
        where TSelf : class
        where TNotify : class
    {
        ThrowIfGroupIsInvalid();

        NotifyDelegateLink delegateLink = NotifyManager.NewDelegateLink();
        delegateLink.InitMethod(func, @this, hasArg: false);
        NotifyManager.AddListenerToGroup(_groupId, typeof(TNotify), delegateLink);
    }

    [XLua.BlackList]
    public unsafe void Listen<TSelf, TNotify>(TSelf @this, delegate*<TSelf, TNotify, void> func)
        where TSelf : class
        where TNotify : class
    {
        ThrowIfGroupIsInvalid();

        NotifyDelegateLink delegateLink = NotifyManager.NewDelegateLink();
        delegateLink.InitMethod(func, @this, hasArg: true);
        NotifyManager.AddListenerToGroup(_groupId, typeof(TNotify), delegateLink);
    }
}
