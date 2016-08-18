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

            createSchema();
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

        public void insertDataCollection(String attribute_key, List<Tuple<String, double>> values) {

        }

        private void createAttribute(String attribute_key) {

        }

        public int getAttributeId(String attribute_key) {
            return 0;
        }

        private void createSchema() {
            createDataSetTable();
            createTaskTable();
            createAttributeKeyTable();
            createAttributeValueTable();
        }

        #region internal

        private void createDataSetTable() {
            ddlQuery(
                "CREATE TABLE IF NOT EXISTS data_set (id INT NOT NULL AUTO_INCREMENT,"
                + "participant_name varchar(30) NOT NULL,"
                + "date DATETIME NOT NULL);"
            );
        }

        private void createAttributeKeyTable() {
            ddlQuery(
                "CREATE TABLE IF NOT EXISTS attribute_key (id INT NOT NULL AUTO_INCREMENT,"
                + "name varchar(30) NOT NULL,"
                + "description varchar(100) NOT NULL);"
            );
        }

        private void createTaskTable()
        {
            ddlQuery(
                "CREATE TABLE IF NOT EXISTS task (id INT NOT NULL AUTO_INCREMENT,"
                + "name VARCHAR(15) NOT NULL"
                + ");"
            );
        }

        private void createAttributeValueTable() {
            ddlQuery(
                "CREATE TABLE IF NOT EXISTS attribute_value (id INT NOT NULL AUTO_INCREMENT,"
                + "attribute_id INT NOT NULL,"
                + "data_set_id INT NOT NULL,"
                + "task_id INT NOT NULL,"
                + "value DOUBLE NOT NULL,"
                + "attribute_id INTEGER REFERENCES attribute_key(id) ON UPDATE CASCADE,"
                + "task_id INTEGER REFERENCES task(id) ON UPDATE CASCADE,"
                + "data_set_id INTEGER REFERENCES data_set(id) ON UPDATE CASCADE"
                + ");"
            );
        }

        private void createRouteTable() {
            ddlQuery(
                "CREATE TABLE IF NOT EXISTS attribute_value (id INT NOT NULL AUTO_INCREMENT,"
                + "name VARCHAR(15) NOT NULL"
                + ");"
            );
        }

        private void ddlQuery(String sql) {
            // open DB connection
            m_dbConnection.Open();

            // table creation query object
            SQLiteCommand creationSQL = new SQLiteCommand(sql);

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

        #endregion 
    }
}
