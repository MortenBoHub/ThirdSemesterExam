using System;
using System.Collections.Generic;

namespace dataccess;

public partial class Player
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phonenumber { get; set; } = null!;

    public string Passwordhash { get; set; } = null!;

    public DateTime Createdat { get; set; }

    public decimal Funds { get; set; }

    public bool Isdeleted { get; set; }

    public virtual ICollection<Playerboard> Playerboards { get; set; } = new List<Playerboard>();

    public virtual ICollection<Fundrequest> Fundrequests { get; set; } = new List<Fundrequest>();
}
