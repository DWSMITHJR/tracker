using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Tracker.Client.Models
{
    [DataContract]
    public class EditIncidentModel
    {
        [Required(ErrorMessage = "ID is required")]
        [DataMember]
        public string? Id { get; set; }
        
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        [DataMember]
        public string? Title { get; set; }

        [DataMember]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [DataMember]
        public string? Status { get; set; }

        [Required(ErrorMessage = "Priority is required")]
        [DataMember]
        public string? Priority { get; set; }

        [DataMember]
        public string? Type { get; set; }

        [DataMember]
        public string? AssignedTo { get; set; }

        [DataMember]
        public string? IndividualId { get; set; }

        [DataMember]
        public string? OrganizationId { get; set; }
    }
}
