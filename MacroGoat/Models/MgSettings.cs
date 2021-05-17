using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MacroGoat.Models
{
    public class MgSettings
    {
        public Option_DeleteFilesAfterSign DeleteFilesAfterSign { get; set; }

        public Section_FileSettings FileSettings { get; set; }
        public Section_ApiSettings ApiSettings { get; set; }

        public class Option_DeleteFilesAfterSign
        {
            [Required]
            [StringLength(5)]
            public string Value { get; set; }
            
            [StringLength(300)]
            public string Description { get; set; }

        }


        public class Section_FileSettings
        {
            
            public Option_ProfilePicturePath ProfilePicturePath { get; set; }
        }

       
        public class Option_ProfilePicturePath
        {
            [Required]
            [StringLength(50)]
            public string Value { get; set; }
            
            [StringLength(300)]
            public string Description { get; set; }
        }

        public class Section_ApiSettings
        {
            public Option_AdhocSignerURL AdhocSignerURL { get; set; }
            public Option_SignerURL SignerURL { get; set; }
            public Option_VerifyURL VerifyURL { get; set; }
            public Option_StatusURL StatusURL { get; set; }

        }

        public class Option_AdhocSignerURL
        {
            [Required]
            [StringLength(100)]
            public string Value { get; set; }

            [StringLength(500)]
            public string Description { get; set; }
        }

        public class Option_SignerURL
        {
            [Required]
            [StringLength(100)]
            public string Value { get; set; }

            [StringLength(500)]
            public string Description { get; set; }
        }

        public class Option_VerifyURL
        {
            [Required]
            [StringLength(100)]
            public string Value { get; set; }

            [StringLength(500)]
            public string Description { get; set; }
        }

        public class Option_StatusURL
        {
            [Required]
            [StringLength(100)]
            public string Value { get; set; }

            [StringLength(500)]
            public string Description { get; set; }
        }




    }
}
