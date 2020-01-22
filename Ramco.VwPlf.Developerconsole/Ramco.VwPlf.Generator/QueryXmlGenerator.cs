//------------------------------------------------------------------------
//  Case ID             : TECH-16278
//  Created By          : Madhan Sekar M
//  Reason For Change   : Implementing QueryXml Generation for MHUB2
//  Modified On         : 24-Nov-2017
//------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ramco.VwPlf.Generator
{
    class QueryXmlGenerator
    {
        Common common = null;
        public QueryXmlGenerator()
        {
            common = new Common();
        }

        public bool Generate()
        {
            ListObject forCatch = null;
            bool bSuccessFlg = false;
            try
            {
                foreach (ListObject htm in GlobalVar.Ui)
                {
                    forCatch = htm;
                    new QueryXmlMetadata(GlobalVar.ConnectionString, htm.ID, htm.Value).saveAsXml();
                }

                bSuccessFlg = true;
            }
            catch(Exception ex) 
            {
                string errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message ;
                common.WriteProfiler($"Error while generating queryxml for :{forCatch.ID} - {forCatch.Value}");
                common.WriteProfiler(errorMessage);
                bSuccessFlg = false;
            }
            return bSuccessFlg;
        }
    }
}
