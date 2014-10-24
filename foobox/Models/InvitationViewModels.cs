using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FooBox.Models
{

    public class EditInvitationsViewModel
    {
        public EditInvitationsViewModel()
        {
            FolderInvitations = new List<InvitationVM>();
            UsersToInvite = new List<EntitySelectedViewModel>();
            GroupsToInvite = new List<EntitySelectedViewModel>();
        }

        public string FullName { get; set; }

        public string FromPath { get; set; }

        public List<InvitationVM> FolderInvitations { get; set; }

        [DisplayName("Users")]
        public List<EntitySelectedViewModel> UsersToInvite { get; set; }

        [DisplayName("Group")]
        public List<EntitySelectedViewModel> GroupsToInvite { get; set; }

    }


    public class AllInvitationsViewModel
    {
        public AllInvitationsViewModel()
        {

            Incoming = new List<InvitationVM>();
            Outgoing = new List<InvitationVM>();
        }

        public List<InvitationVM> Incoming { get; set; }
        public List<InvitationVM> Outgoing { get; set; }
    }


    public class InvitationVM
    {
        public string FolderName { get; set; }
        public long Id { get; set; }

        public string UserName { get; set; }

        public string GroupName { get; set; }

        public bool Accepted { get; set; }

        public DateTime Timestamp { get; set; }

    }



}