using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Globalization;

namespace ModForResearchTUB
{
    class DBI
    {
        SQLiteConnection m_dbConnection;

        public DBI() {
            m_dbConnection =
            new SQLiteConnection("Data Source=mod4researchTUB.db;Version=3;");

            createScheme();
        }

        public long createDataset(String participant_name) {
            // open DB connection
            m_dbConnection.Open();

            // get current date, to format as insert parameter
            DateTime current_date = new DateTime();

            // create the actual insert query and set parameters
            SQLiteCommand insertSQL = new SQLiteCommand(
                "INSERT INTO data_set (participant_name, date) VALUES (?,?)", m_dbConnection);
            insertSQL.Parameters.Add(participant_name);
            insertSQL.Parameters.Add(current_date.ToString("yyyy-MM-dd HH:mm:ss",
                  CultureInfo.InvariantCulture));

            // execute query
            try
            {
                // data set insert was successful
                if (insertSQL.ExecuteNonQuery() == 1) {
                    return m_dbConnection.LastInsertRowId;
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally {
                // close DB connection
                m_dbConnection.Close();
            }

            return 0;
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
            // open DB connection
            m_dbConnection.Open();

            // table creation query object
            SQLiteCommand creationSQL = new SQLiteCommand(
                "CREATE TABLE IF NOT EXISTS data_set (id INT NOT NULL AUTO_INCREMENT,"
                + "participant_name varchar(30) NOT NULL,"
                + "date DATETIME NOT NULL);"
            );

            // execute query
            try
            {
                creationSQL.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                // close DB connection
                m_dbConnection.Close();
            }
        }

        private void createAttributeKeyTable() {
            // open DB connection
            m_dbConnection.Open();

            // table creation query object
            SQLiteCommand creationSQL = new SQLiteCommand(
                "CREATE TABLE IF NOT EXISTS attribute_key (id INT NOT NULL AUTO_INCREMENT,"
                + "name varchar(30) NOT NULL,"
                + "description varchar(100) NOT NULL);"
            );

            // execute query
            try
            {
                creationSQL.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                // close DB connection
                m_dbConnection.Close();
            }
        }

        private void createAttributeValueTable() {

        }

        #endregion 
    }
}
