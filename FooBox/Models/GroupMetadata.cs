using System;
using System.ComponentModel.DataAnnotations;

namespace FooBox
{
    [MetadataType(typeof(GroupMetadata))]
    public partial class Group
    {
    }

    public partial class GroupMetadata
    {
        [Required(ErrorMessage = "Please enter : Id")]
        [Display(Name = "Id")]
        public long Id { get; set; }

        [Display(Name = "Name")]
        public string Name { get; set; }

        [Display(Name = "State")]
        public ObjectState State { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Is Admin")]
        public bool IsAdmin { get; set; }

        [Display(Name = "Users")]
        public User Users { get; set; }

    }
}
