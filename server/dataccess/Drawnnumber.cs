using System;
using System.Collections.Generic;

namespace dataccess;

public partial class Drawnnumber
{
    public string Id { get; set; } = null!;

    public string Boardid { get; set; } = null!;

    public int Drawnnumber1 { get; set; }

    public DateTime Drawnat { get; set; }

    public string Drawnby { get; set; } = null!;

    public virtual Board Board { get; set; } = null!;

    public virtual Admin DrawnbyNavigation { get; set; } = null!;
}
