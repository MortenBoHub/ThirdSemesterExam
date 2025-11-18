using System;
using System.Collections.Generic;

namespace dataccess;

public partial class Board
{
    public string Id { get; set; } = null!;

    public int Weeknumber { get; set; }

    public int Year { get; set; }

    public DateTime Startdate { get; set; }

    public DateTime Enddate { get; set; }

    public bool Isactive { get; set; }

    public DateTime Createdat { get; set; }

    public virtual ICollection<Drawnnumber> Drawnnumbers { get; set; } = new List<Drawnnumber>();

    public virtual ICollection<Playerboard> Playerboards { get; set; } = new List<Playerboard>();
}
