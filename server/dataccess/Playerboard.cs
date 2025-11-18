using System;
using System.Collections.Generic;

namespace dataccess;

public partial class Playerboard
{
    public string Id { get; set; } = null!;

    public string Playerid { get; set; } = null!;

    public string Boardid { get; set; } = null!;

    public DateTime Createdat { get; set; }

    public bool Iswinner { get; set; }

    public virtual Board Board { get; set; } = null!;

    public virtual Player Player { get; set; } = null!;

    public virtual ICollection<Playerboardnumber> Playerboardnumbers { get; set; } = new List<Playerboardnumber>();
}
