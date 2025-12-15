using System;
using System.Collections.Generic;

namespace dataccess;

public partial class Fundrequest
{
    public string Id { get; set; } = null!;

    public string Playerid { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Transactionnumber { get; set; } = null!;

    public string Status { get; set; } = null!; // pending | approved | denied

    public DateTime Createdat { get; set; }

    public DateTime? Processedat { get; set; }

    public string? Processedbyadminid { get; set; }

    public virtual Player Player { get; set; } = null!;

    public virtual Admin? Processedbyadmin { get; set; }
}
