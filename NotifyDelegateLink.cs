using System;

internal sealed unsafe class NotifyDelegateLink
{
    private enum InvokeType
    {
        None = 0,

        Delegate_ZeroArg,
        Delegate_OneArg,

        FuncPtr_ZeroArg,
        FuncPtr_OneArg,

        FuncPtr_This_ZeroArg,
        FuncPtr_This_OneArg,
    }

    private InvokeType _invokeType;
    private void* _funcPtr;
    private object _delegateOrThis;

    public uint GroupId;
    public NotifyDelegateLink GroupOrFreeNext;

    public NotifyDelegateLink NotifyPrev;
    public NotifyDelegateLink NotifyNext;

    public void ResetMethod()
    {
        _invokeType = InvokeType.None;
        _funcPtr = null;
        _delegateOrThis = null;
    }

    public void ResetNotify()
    {
        NotifyPrev = null;
        NotifyNext = null;
    }

    public void InitMethod(NotifyZeroArgDelegate method)
    {
        _invokeType = InvokeType.Delegate_ZeroArg;
        _delegateOrThis = method;
    }

    public void InitMethod(NotifyOneArgDelegate method)
    {
        _invokeType = InvokeType.Delegate_OneArg;
        _delegateOrThis = method;
    }

    public void InitMethod(void* funcPtr, bool hasArg)
    {
        _invokeType = hasArg ? InvokeType.FuncPtr_OneArg : InvokeType.FuncPtr_ZeroArg;
        _funcPtr = funcPtr;
    }

    public void InitMethod(void* funcPtr, object @this, bool hasArg)
    {
        _invokeType = hasArg ? InvokeType.FuncPtr_This_OneArg : InvokeType.FuncPtr_This_ZeroArg;
        _funcPtr = funcPtr;
        _delegateOrThis = @this;
    }

    public void SetGroupIdAndPrependToLinkedList(uint groupId, NotifyDelegateLink groupListHead, NotifyDelegateLink notifyListHead)
    {
        GroupId = groupId;

        // groupList
        GroupOrFreeNext = groupListHead.GroupOrFreeNext;
        groupListHead.GroupOrFreeNext = this;

        // notifyList
        NotifyPrev = notifyListHead;
        NotifyNext = notifyListHead.NotifyNext;
        notifyListHead.NotifyNext = this;

        if (NotifyNext != null)
        {
            NotifyNext.NotifyPrev = this;
        }
    }

    public void Invoke(object arg)
    {
        switch (_invokeType)
        {
            case InvokeType.None:
                throw new InvalidOperationException($"Invoking an uninitialized {nameof(NotifyDelegateLink)} object!");

            case InvokeType.Delegate_ZeroArg:
                ((NotifyZeroArgDelegate)_delegateOrThis)();
                break;

            case InvokeType.Delegate_OneArg:
                ((NotifyOneArgDelegate)_delegateOrThis)(arg);
                break;

            case InvokeType.FuncPtr_ZeroArg:
                ((delegate*<void>)_funcPtr)();
                break;

            case InvokeType.FuncPtr_OneArg:
                ((delegate*<object, void>)_funcPtr)(arg);
                break;

            case InvokeType.FuncPtr_This_ZeroArg:
                ((delegate*<object, void>)_funcPtr)(_delegateOrThis);
                break;

            case InvokeType.FuncPtr_This_OneArg:
                ((delegate*<object, object, void>)_funcPtr)(_delegateOrThis, arg);
                break;

            default:
                throw new NotSupportedException($"Invoking an unsupported {nameof(NotifyDelegateLink)} object!");
        }
    }
}
