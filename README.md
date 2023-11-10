# UnityNotifyManager

Unity 通知/事件管理，基于类型，不需要额外维护字符串或者枚举，支持 xLua。

默认依赖了我写的 [EasyTypeReload](https://github.com/stalomeow/EasyTypeReload) 和腾讯的 [xLua](https://github.com/Tencent/xLua)。不需要它们的话，把相关的 Attribute 删了就行。

## C# 使用

``` csharp
public class TestNotify : IDisposable
{
    public string Message;
    public int Id;

    public void Dispose()
    {
        Message = null;
        Id = 0;
    }
}

public class Example : MonoBehaviour
{
    private NotifyGroup _notifyGroup;

    private void Start()
    {
        unsafe
        {
            // 创建一个组，用来监听一组事件
            _notifyGroup = NotifyManager.NewGroup(nameof(Example));

            // 利用托管函数指针注册 “实例方法”
            // 第一个泛型参数是 this 的类型
            // 第二个泛型参数是 要监听的事件 的类型
            // 这样注册的话，不会额外创建委托
            _notifyGroup.Listen<Example, string>(this, &OnTextNotify);
            _notifyGroup.ListenNoArg<Example, string>(this, &OnTextNotifyNoArg);
            _notifyGroup.Listen<Example, TestNotify>(this, &OnTestNotify);

            // 利用托管函数指针注册静态方法
            // 第一个泛型参数是 要监听的事件 的类型
            // 这样注册的话，不会额外创建委托
            _notifyGroup.Listen<TestNotify>(&OnTestNotifyStatic);
            _notifyGroup.ListenNoArg<TestNotify>(&OnTestNotifyStaticNoArg);

            // 利用委托注册方法
            // 第一个参数是 要监听的事件 的类型，必须是引用类型
            _notifyGroup.Listen(typeof(TestNotify), OnTestNotifyDelegate);
            _notifyGroup.ListenNoArg(typeof(TestNotify), OnTestNotifyDelegateNoArg);
        }

        SendNotify();

        // 取消注册
        _notifyGroup.Dispose();
        _notifyGroup = default;
    }

    private void SendNotify()
    {
        // 直接传一个 string 对象，触发 string 事件
        // 这个方法 take 一个 object 参数，不是泛型方法
        NotifyManager.Send("Hello World!");

        // 使用一个缓存的 TestNotify 对象触发 TestNotify 事件
        NotifyManager.SendEmpty<TestNotify>();
        NotifyManager.SendEmpty(typeof(TestNotify));

        // 延迟触发 TestNotify 事件，testNotify 是内置的对象池中的对象
        // 事件的类型必须实现 IDisposable 接口，在 Dispose() 方法里重置对象
        using (NotifyManager.SendDeferred(out TestNotify testNotify))
        {
            // 在这里设置字段
            testNotify.Id = 1234;
            testNotify.Message = "From C#";

            // 在这里结束时触发事件，回收 testNotify
        }
    }

    // “实例方法” 的第一个参数是 this (self)
    private static void OnTextNotify(Example self, string text)
    {
        Debug.Log("c# string-notify: " + text, self);
    }

    // “实例方法” 的第一个参数是 this (self)
    private static void OnTextNotifyNoArg(Example self)
    {
        Debug.Log("c# string-notify-no-arg", self);
    }

    // “实例方法” 的第一个参数是 this (self)
    private static void OnTestNotify(Example self, TestNotify notify)
    {
        Debug.Log($"c# test-notify: Id={notify.Id}, Msg='{notify.Message}'", self);
    }

    private static void OnTestNotifyStatic(TestNotify notify)
    {
        Debug.Log($"c# test-notify-static: Id={notify.Id}, Msg='{notify.Message}'");
    }

    private static void OnTestNotifyStaticNoArg()
    {
        Debug.Log($"c# test-notify-static-no-arg");
    }

    // 参数类型必须是 object
    private void OnTestNotifyDelegate(object arg)
    {
        TestNotify notify = (TestNotify)arg;
        Debug.Log($"c# test-notify-delegate: Id={notify.Id}, Msg='{notify.Message}'", this);
    }

    private static void OnTestNotifyDelegateNoArg()
    {
        Debug.Log($"c# test-notify-delegate-no-arg");
    }
}
```

## Lua 使用

``` lua
local notifyGroup = nil

function registerNotify()
    -- 创建一个组，用来监听一组事件
    notifyGroup = CS.NotifyManager.NewGroup('Lua Group')

    --
    -- 注册同 C#，但不能用函数指针
    --

    notifyGroup:Listen(typeof(CS.TestNotify), function(notify)
        print(string.format('lua test-notify: Id=%d, Msg=\'%s\'', notify.Id, notify.Message))
    end)

    notifyGroup:Listen(typeof(CS.System.String), function(notify)
        print('lua string-notify: ' .. notify)
    end)

    notifyGroup:ListenNoArg(typeof(CS.System.String), function()
        print('lua string-notify-no-arg')
    end)
end

function sendNotify()
    -- 直接传一个 TestNotify 对象，触发 TestNotify 事件
    local notify = CS.TestNotify()
    notify.Id = 5678
    notify.Message = 'from lua'
    CS.NotifyManager.Send(notify)

    -- 使用一个缓存的 TestNotify 对象触发 TestNotify 事件
    CS.NotifyManager.SendEmpty(typeof(TestNotify));
end

function unregisterNotify()
    -- 取消注册
    notifyGroup:Dispose()
    notifyGroup = nil
end
```
