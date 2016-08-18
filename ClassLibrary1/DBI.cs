using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace ModForResearchTUB
{
    class DBI
    {
        public void createDataset(String participant_name) {

        }

        public void insertValue(String attribute_key, double value) {

        }

        private void createAttribute(String attribute_key) {

        }

        public int getAttributeId(String attribute_key) {
            return 0;
        }

        private void createScheme() {
            createDataSetTable();
            createAttributeKeyTable();
            createAttributeValueTable();
        }

        #region internal

        private void createDataSetTable() {

        }

        private void createAttributeKeyTable() {
        }

        private void createAttributeValueTable() {

        }

        #endregion 
    }
}
