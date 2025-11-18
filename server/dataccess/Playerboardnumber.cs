using System;
using System.Collections.Generic;

namespace dataccess;

public partial class Playerboardnumber
{
    public string Id { get; set; } = null!;

    public string Playerboardid { get; set; } = null!;

    public int Selectednumber { get; set; }

    public virtual Playerboard Playerboard { get; set; } = null!;
}
