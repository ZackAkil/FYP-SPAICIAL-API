//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Spaicial_API.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Feature
    {
        public int predictedZoneId { get; set; }
        public int predictedDataSubjectId { get; set; }
        public int sourceZoneId { get; set; }
        public int sourceDataSubjectId { get; set; }
        public int expValue { get; set; }
        public double multiValue { get; set; }
    
        public virtual DataSubject DataSubject { get; set; }
        public virtual DataSubject DataSubject1 { get; set; }
        public virtual Zone Zone { get; set; }
        public virtual Zone Zone1 { get; set; }
    }
}
