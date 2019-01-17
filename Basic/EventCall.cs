using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

public delegate void EventCallType(EventParams envParams, params Object[] parameters);
public class EventCall {
    #region members
    EventCallType method;
    object[] parameters;
    public EventCall NextCall;
    #endregion

    public EventCall(EventCallType method, object[] parameters) {
        this.method = method;
        this.parameters = parameters;
        NextCall = null;
    }

    public void Execute() {
        Execute(null);
    }

    public void Execute(EventParams eventParams) {
        method(eventParams, parameters);
        if (NextCall != null)
            NextCall.Execute(eventParams);
    }
}