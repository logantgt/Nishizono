using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nishizono.Database.Models;

public class ProviderQuery
{
    public required int Id { get; set; }
    public required string Query { get; set; }
    public required string Provider { get; set; }
    public required MetadataType Type { get; set; }
    public required DateTime InvalidAt { get; set; }
}
