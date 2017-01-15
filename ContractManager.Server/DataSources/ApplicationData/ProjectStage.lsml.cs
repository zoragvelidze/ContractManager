using Microsoft.LightSwitch;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System;

namespace LightSwitchApplication
{
    public partial class ProjectStage
    {
        partial void CloseDate_Validate(EntityValidationResultsBuilder results)
        {
            if (this.Closed && !this.CloseDate.HasValue)
                results.AddPropertyError("არ არის შევსებული დახურვის თარიღი");

        }

    }
}