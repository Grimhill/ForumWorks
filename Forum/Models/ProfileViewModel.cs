using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace Forum.Models
{
    public class ProfileViewModel
    {
        [Display(Name = "First Name")]
        public string UserName { get; set; }

        public string UserRole { get; set; }

        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "Country")]
        public string Country { get; set; }

        [Display(Name = "City")]
        public string City { get; set; }

        [Display(Name = "Gender")]
        public string Gender { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "BirthDate (MM/dd/yyyy)")]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "About Yourself")]
        public string YourSelfDescription { get; set; }

        public DateTime Registered { get; set; }
        public DateTime LastLogin { get; set; }

        public bool OnlineStatus { get; set; }
    }
}