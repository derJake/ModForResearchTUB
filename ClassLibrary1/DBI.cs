using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Globalization;

namespace ModForResearchTUB
{
    class DBI
    {
        SqlConnection m_dbConnection;

        public DBI() {
            m_dbConnection =
            new SqlConnection("user id=TUBMod4Research;" +
                                "password=123456;server=localhost;" +
                                "Trusted_Connection=yes;" +
                                "database=TUBMod4Research; " +
                                "connection timeout=30");

            if (m_dbConnection != null)
            {
                try
                {
                    m_dbConnection.Open();
                    m_dbConnection.Close();
                    createSchema();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }

        public int createDataset(String participant_name) {
            // open DB connection
            m_dbConnection.Open();

            // get current date, to format as insert parameter
            DateTime current_date = new DateTime();

            // create the actual insert query and set parameters
            SqlCommand insertSQL = new SqlCommand(
                "INSERT INTO data_set (participant_name, date) OUTPUT INSERTED.ID VALUES (?,?)", m_dbConnection);
            insertSQL.Parameters.Add(participant_name);
            insertSQL.Parameters.Add(current_date.ToString("yyyy-MM-dd HH:mm:ss",
                  CultureInfo.InvariantCulture));

            // execute query
            try
            {
                // data set insert was successful
                return (Int32)insertSQL.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally {
                // close DB connection
                m_dbConnection.Close();
            }
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
                + "attribute_id INTEGER REFERENCES attribute_key(id) ON UPDATE CASCADE ON DELETE CASCADE,"
                + "task_id INTEGER REFERENCES task(id) ON UPDATE CASCADE ON DELETE CASCADE,"
                + "data_set_id INTEGER REFERENCES data_set(id) ON UPDATE CASCADE ON DELETE CASCADE"
                + ");"
            );
        }

        private void createRouteTable() {
            ddlQuery(
                "CREATE TABLE IF NOT EXISTS route (id INT NOT NULL AUTO_INCREMENT,"
                + "name VARCHAR(15) NOT NULL"
                + ");"
            );
        }

        private void createCheckpointsTable()
        {
            ddlQuery(
                "CREATE TABLE IF NOT EXISTS route_checkpoints (id INT NOT NULL AUTO_INCREMENT,"
                + "route_id INT NOT NULL,"
                + "x FLOAT NOT NULL,"
                + "y FLOAT NOT NULL,"
                + "z FLOAT NOT NULL,"
                + "type INT(1) NOT NULL DEFAULT 0"
                + "route_id INTEGER REFERENCES route(id) ON UPDATE CASCADE ON DELETE CASCADE"
                + ");"
            );
        }

        private void ddlQuery(String sql) {
            // open DB connection
            m_dbConnection.Open();

            // table creation query object
            SqlCommand creationSQL = new SqlCommand(sql);

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
