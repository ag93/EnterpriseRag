using System;
using System.Collections.Generic;
using System.Text;

namespace EnterpriseRag.Application.Abstractions
{
    public interface ICurrentUser
    {
        bool IsAuthenticated { get; }
        IReadOnlyCollection<string> Groups { get; }
    }
}
