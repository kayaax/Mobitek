using System;

namespace Grand.Plugin.Payments.AllBank.Iyzico
{
    public interface RequestStringConvertible
    {
        String ToPKIRequestString();
    }
}
