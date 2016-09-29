using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CostOfOutsideProcesses
{
    class Globals
    {
        public static readonly string odbc_connection_string = "DSN=jobboss32;UID=jbread;PWD=Cloudy2Day";
        public static readonly string binding_connection_string = "Server=ATI-SQL;Database=uniPoint_Live;UID=jbread;PWD=Cloudy2Day";
        public static readonly string generic_IT_error = "Problem connection to database server. Please contact IT support.";
    }
}
