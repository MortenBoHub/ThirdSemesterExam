using System;
using System.Collections.Generic;

namespace dataccess;

public partial class Admin
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phonenumber { get; set; } = null!;

    public string Passwordhash { get; set; } = null!;

    public DateTime Createdat { get; set; }

    public bool Isdeleted { get; set; }

    public virtual ICollection<Drawnnumber> Drawnnumbers { get; set; } = new List<Drawnnumber>();
}
