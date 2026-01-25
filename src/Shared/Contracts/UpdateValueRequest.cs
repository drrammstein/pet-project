using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts;

public class UpdateValueRequest
{
    public string NewValue { get; set; } = string.Empty;
}