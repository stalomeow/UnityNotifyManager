using System;

[XLua.CSharpCallLua]
[XLua.LuaCallCSharp]
public delegate void NotifyExceptionHandler(Type notifyType, string groupName, Exception exception);
