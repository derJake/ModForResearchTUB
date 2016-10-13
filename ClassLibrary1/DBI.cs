using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Globalization;
using System.Data;
using GTA.Math;

namespace ModForResearchTUB
{
    class DBI
    {
        SqlConnection m_dbConnection;
        String defaultHost = "localhost";
        char[] trim = { ',', ' '};

        public DBI(String dbHost) {
            String hostName = defaultHost;
            if (!dbHost.Equals("")) {
                hostName = dbHost;
            }

            m_dbConnection =
            new SqlConnection("user id=TUBMod4Research;" +
                                "password=123456;server=" +
                                hostName +
                                ";" +
                                "Trusted_Connection=yes;" +
                                "database=TUBMod4Research; " +
                                "connection timeout=30");

            try
            {
                m_dbConnection.Open();
                m_dbConnection.Close();
                createSchema();
            }
            catch (Exception ex)
            {
                Logger.Log(ex.StackTrace);
                Logger.Log(ex.Message);
            }
        }

        public int createDataset(String participant_name) {
            // create the actual insert query and set parameters
            SqlCommand insertSQL = new SqlCommand(
                "INSERT INTO data_set (participant_name, date) OUTPUT INSERTED.ID VALUES (@name,@date)", m_dbConnection);
            insertSQL.Parameters.Add(new SqlParameter("@name", participant_name));
            insertSQL.Parameters.Add("@date", SqlDbType.DateTime);
            insertSQL.Parameters["@date"].Value = DateTime.Now;

            try
            {
                // open DB connection
                m_dbConnection.Open();
                return (Int32)insertSQL.ExecuteScalar();
            }
            catch (Exception ex) {
                Logger.Log(ex.StackTrace);
                Logger.Log(ex.Message);
            }
            finally {
                m_dbConnection.Close();
            }
            return 0;
        }

        public int insertValue(String attribute_key, int task_id, int data_set_id, double value) {
            int attribute_id = getAttributeId(attribute_key);
            if (attribute_id > 0) {
                String sql = "INSERT INTO dbo.attribute_value (attribute_id, task_id, data_set_id, value)" 
                    + "VALUES(@attributeId, @taskId, @dataSetId, @value)";
                SqlCommand cmd = new SqlCommand(sql, m_dbConnection);
                cmd.Parameters.AddWithValue("@attributeId", attribute_id);
                cmd.Parameters.AddWithValue("@taskId", task_id);
                cmd.Parameters.AddWithValue("@dataSetId", data_set_id);
                cmd.Parameters.AddWithValue("@value", value);
                return insert(cmd);
            }
            return 0;
        }

        public int insertCheckpoint(int task_id, Vector3 coordsNormal, Vector3? coordsAlt)
        {
            String sql = "INSERT INTO dbo.task_checkpoint (task_id, x_normal, y_normal, z_normal, x_alt, y_alt, z_alt)"
                + "VALUES(@taskId, @coordsNormalX, @coordsNormalY, @coordsNormalZ, @coordsAltX, @coordsAltY, @coordsAltZ)";
            SqlCommand cmd = new SqlCommand(sql, m_dbConnection);
            cmd.Parameters.AddWithValue("@taskId", task_id);
            cmd.Parameters.AddWithValue("@coordsNormalX", coordsNormal.X);
            cmd.Parameters.AddWithValue("@coordsNormalY", coordsNormal.Y);
            cmd.Parameters.AddWithValue("@coordsNormalZ", coordsNormal.Z);

            if (coordsAlt.HasValue)
            {
                cmd.Parameters.AddWithValue("@coordsAltX", coordsAlt.Value.X);
                cmd.Parameters.AddWithValue("@coordsAltY", coordsAlt.Value.Y);
                cmd.Parameters.AddWithValue("@coordsAltZ", coordsAlt.Value.Z);
            }
            else {
                cmd.Parameters.AddWithValue("@coordsAltX", DBNull.Value);
                cmd.Parameters.AddWithValue("@coordsAltY", DBNull.Value);
                cmd.Parameters.AddWithValue("@coordsAltZ", DBNull.Value);
            }
            return insert(cmd);
        }

        private int insert(SqlCommand cmd) {
            try
            {
                m_dbConnection.Open();
                int numOfRows = cmd.ExecuteNonQuery();
                return numOfRows;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.StackTrace);
                Logger.Log(ex.Message);
                Logger.Log("Query: " + cmd.CommandText);
                for (int i = 0; i < cmd.Parameters.Count; i++) {
                    Logger.Log(cmd.Parameters[i].ParameterName.ToString() + ": " + cmd.Parameters[i].Value.ToString());
                }
            }
            finally
            {
                m_dbConnection.Close();
            }
            return 0;
    }

        public int insertDataCollection(int attribute_id, int task_id, int data_set_id, Dictionary<String, double> values) {
            String sql = "INSERT INTO dbo.attribute_value (attribute_id, task_id, data_set_id, value) VALUES";
            String currentInsert = sql;
            int numRows = 0;
            int i = 0;

            foreach (KeyValuePair<String, double> entry in values) {
                currentInsert += "('" + attribute_id + "', '" + task_id + "', '" + data_set_id + "', '" + entry.Value.ToString(CultureInfo.InvariantCulture) + "'),";
                i++;

                // cut off
                if (i == 999) {
                    currentInsert = currentInsert.TrimEnd(trim);
                    numRows += insertDataSubset(currentInsert);
                    currentInsert = sql;
                    i = 0;
                }
                
            }

            return numRows;
        }

        private int insertDataSubset(String sql) {
            SqlCommand cmd = new SqlCommand(sql, m_dbConnection);
            cmd.CommandTimeout = 600;

            try
            {
                if (m_dbConnection.State != ConnectionState.Open)
                {
                    m_dbConnection.Open();
                }
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.Log(ex.StackTrace);
                Logger.Log(ex.Message);
                Logger.Log("sql: " + sql.Substring(sql.Length - 50));
            }
            finally
            {
                if (m_dbConnection.State == ConnectionState.Open)
                {
                    m_dbConnection.Close();
                }
            }
            return 0;
        }

        public int createTask(String name) {
            String sql = "INSERT INTO task (name) OUTPUT INSERTED.ID VALUES(@taskName)";
            SqlCommand cmd = new SqlCommand(sql, m_dbConnection);
            cmd.Parameters.AddWithValue("@taskName", name);

            m_dbConnection.Open();
            int taskId = (Int32)cmd.ExecuteScalar();
            m_dbConnection.Close();
            return taskId;
        }

        public int getTaskIdByName(String name) {
            String sql = "SELECT id FROM dbo.task WHERE name LIKE @taskName";
            SqlCommand cmd = new SqlCommand(sql, m_dbConnection);
            cmd.Parameters.AddWithValue("@taskName", name);
            SqlDataReader reader;

            try {
                m_dbConnection.Open();

                reader = cmd.ExecuteReader();
                // Data is accessible through the DataReader object here.

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        return reader.GetInt32(0);
                    }
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                Logger.Log(ex.StackTrace);
                Logger.Log(ex.Message);
            }
            finally
            {
                m_dbConnection.Close();
            }

            return 0;
        }

        public int createAttribute(String attribute_key, String attribute_description) {
            int id = 0;
            String sql = "INSERT INTO dbo.attribute_key (name, description) OUTPUT INSERTED.ID VALUES(@attributeName, @attributeDescription)";
            SqlCommand cmd = new SqlCommand(sql, m_dbConnection);
            cmd.Parameters.AddWithValue("@attributeName", attribute_key);
            cmd.Parameters.AddWithValue("@attributeDescription", attribute_description);

            try
            {
                m_dbConnection.Open();
                id = (Int32)cmd.ExecuteScalar();
                
            }
            catch (SqlException e)
            {
                Logger.Log("attribute_key: " + attribute_key);
                Logger.Log(e.StackTrace);
                Logger.Log(e.Message);
            }
            finally {
                m_dbConnection.Close();
            }

            return id;
        }

        public int getAttributeId(String attribute_key) {
            String sql = "SELECT id FROM dbo.attribute_key WHERE name LIKE @attributeName";
            SqlCommand cmd = new SqlCommand(sql, m_dbConnection);
            cmd.Parameters.AddWithValue("@attributeName", attribute_key);
            SqlDataReader reader;

            try {
                m_dbConnection.Open();

                reader = cmd.ExecuteReader();
                // Data is accessible through the DataReader object here.

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        return reader.GetInt32(0);
                    }
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                Logger.Log(ex.StackTrace);
                Logger.Log(ex.Message);
            }
            finally
            {
                m_dbConnection.Close();
            }

            return 0;
        }

        private void createSchema() {
            createDataSetTable();
            createTaskTable();
            createAttributeKeyTable();
            createAttributeValueTable();
            createCheckpointsTable();
        }

        #region internal

        private void createDataSetTable() {
            ddlQuery(
                "IF OBJECT_ID('dbo.data_set', 'U') IS NULL "
                + "CREATE TABLE data_set ("
                + "id INT IDENTITY (1,1) NOT NULL,"
                + "participant_name varchar(30) NOT NULL,"
                + "date DATETIME NOT NULL);"
            );
        }

        private void createAttributeKeyTable() {
            ddlQuery(
                "IF OBJECT_ID('dbo.attribute_key', 'U') IS NULL "
                + "CREATE TABLE attribute_key ("
                + "id INT IDENTITY (1,1) NOT NULL,"
                + "name varchar(30) NOT NULL,"
                + "description varchar(100) NOT NULL);"
            );
        }

        private void createTaskTable()
        {
            ddlQuery(
                "IF OBJECT_ID('dbo.task', 'U') IS NULL "
                + "CREATE TABLE task ( "
                + "id INT IDENTITY (1,1) NOT NULL,"
                + "name VARCHAR(15) NOT NULL"
                + ");"
            );
        }

        private void createAttributeValueTable() {
            ddlQuery(
                "IF OBJECT_ID('dbo.attribute_value', 'U') IS NULL "
                + "CREATE TABLE attribute_value "
                + "("
                + "id INT IDENTITY (1,1) NOT NULL,"
                + "value FLOAT NOT NULL,"
                + "attribute_id INTEGER REFERENCES attribute_key(id) ON UPDATE CASCADE ON DELETE CASCADE,"
                + "task_id INTEGER REFERENCES task(id) ON UPDATE CASCADE ON DELETE CASCADE,"
                + "data_set_id INTEGER REFERENCES data_set(id) ON UPDATE CASCADE ON DELETE CASCADE"
                + ");"
            );
        }

        private void createRouteTable() {
            ddlQuery(
                "IF OBJECT_ID('dbo.route', 'U') IS NULL "
                + "CREATE TABLE route ("
                + "id INT IDENTITY (1,1) NOT NULL,"
                + "name VARCHAR(15) NOT NULL"
                + ");"
            );
        }

        private void createCheckpointsTable()
        {
            ddlQuery(
                "IF OBJECT_ID('dbo.task_checkpoints', 'U') IS NULL "
                + "CREATE TABLE task_checkpoints ("
                + "id INT IDENTITY (1,1) NOT NULL,"
                + "normal_x FLOAT NOT NULL,"
                + "normal_y FLOAT NOT NULL,"
                + "normal_z FLOAT NOT NULL,"
                + "alt_x FLOAT,"
                + "alt_y FLOAT,"
                + "alt_z FLOAT,"
                + "route_id INTEGER REFERENCES task(id) ON UPDATE CASCADE ON DELETE CASCADE"
                + ");"
            );
        }

        private void ddlQuery(String sql) {
            // table creation query object
            SqlCommand creationSQL = new SqlCommand(sql);

            // execute query
            try
            {
                // open DB connection
                m_dbConnection.Open();
                creationSQL.Connection = m_dbConnection;
                creationSQL.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.Log(ex.StackTrace);
                Logger.Log(ex.Message);
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
