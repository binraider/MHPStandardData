using System;
using System.IO;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace MHPStandardData {


    /// <summary>
    /// Summary description for DataAccess.
    /// </summary>
    public class DataAccess : IDisposable {

        //private SqlConnection conn = null;
        private string connectionstring = "";
        private int commandtimeout = 0;

        public DataAccess(string m_connectionstring) {
            try {
                connectionstring = m_connectionstring;
                //conn = new SqlConnection(connectionstring);
            } catch (SqlException ex) {

            } catch (Exception ex) {

            }
        }

        public DataAccess(string m_connectionstring, int m_commandtimeout) {
            try {
                commandtimeout = m_commandtimeout;
                connectionstring = m_connectionstring;
                //conn = new SqlConnection(connectionstring);
            } catch (SqlException ex) {

            } catch (Exception ex) {

            }
        }

        public ReturnClass UploadImageField(string filepath, string imagefieldparametername, string Sql) {
            ReturnClass outcome = new ReturnClass(true);
            SP_Parameters p = new SP_Parameters();
            SqlCommand cmd = null;
            Stream imgStream = null;
            FileInfo file = null;
            byte[] imgBinaryData = null;
            int RowsAffected = 0;
            int filesize = 0;
            int n = 0;

            if (!imagefieldparametername.StartsWith("@")) {
                imagefieldparametername = "@" + imagefieldparametername;
            }
            if (!File.Exists(filepath)) {
                outcome.SetFailureMessage("The file does not exist or is not accessible.");
            }

            if (outcome.Success) {
                try {
                    file = new FileInfo(filepath);
                    filesize = Convert.ToInt32(file.Length);
                } catch (Exception ex) {
                    outcome.Success = false;
                    outcome.Message = ex.Message;
                }
            }

            if (outcome.Success) {
                try {
                    imgStream = File.OpenRead(filepath);
                    imgBinaryData = new byte[filesize];
                    n = imgStream.Read(imgBinaryData, 0, filesize);
                } catch (Exception ex) {
                    outcome.Success = false;
                    outcome.Message = ex.Message;
                }
            }

            if (outcome.Success) {
                try {
                    using (var conn = new SqlConnection(connectionstring)) {
                        conn.Open();
                        cmd = new SqlCommand(Sql, conn);
                        if (commandtimeout > 0) {
                            cmd.CommandTimeout = commandtimeout;
                        }
                        p.Add(imagefieldparametername, SqlDbType.Image, filesize, ParameterDirection.Input, imgBinaryData);
                        foreach (SqlParameter objparam1 in p) {
                            cmd.Parameters.Add(objparam1);
                        }
                        RowsAffected = cmd.ExecuteNonQuery();
                    }
                } catch (Exception ex) {
                    outcome.Success = false;
                    outcome.Message = ex.Message;
                }
            }

            try {
                imgStream.Close();
            } catch (Exception ex) {

            }
            return outcome;
        }

        #region Async Void Methods
        public async Task<ReturnClass> ExecSqlAsync(string strQuery) {
            ReturnClass outcome = await ExecSqlAsyncMethods(strQuery, null, false);
            return outcome;
        }

        public async Task<ReturnClass> ExecSqlParamsAsync(string strQuery, SP_Parameters p) {
            ReturnClass outcome = await ExecSqlAsyncMethods(strQuery, p, false);
            return outcome;
        }

        public async Task<ReturnClass> ExecProcVoidAsync(string procname) {
            ReturnClass outcome = await ExecSqlAsyncMethods(procname, null, true);
            return outcome;
        }

        public async Task<ReturnClass> ExecProcVoidParamsAsync(string procname, SP_Parameters p) {
            ReturnClass outcome = await ExecSqlAsyncMethods(procname, p, true);
            return outcome;
        }

        // TODO https://docs.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlconnection.begintransaction?view=sqlclient-dotnet-core-1.1
        private async Task<ReturnClass> ExecSqlAsyncMethods(string strQuery, SP_Parameters p, bool isProc) {
            ReturnClass outcome = new ReturnClass(true);
            int result = 0;
            string errormsg = "";
            bool isError = false;
            using (var conn = new SqlConnection(connectionstring)) {
                await conn.OpenAsync();
                using (var tran = conn.BeginTransaction()) {
                    using (var command = new SqlCommand(strQuery, conn, tran)) {
                        command.CommandTimeout = commandtimeout;
                        if (isProc) {
                            command.CommandType = CommandType.StoredProcedure;
                        } else {
                            command.CommandType = CommandType.Text;
                        }

                        if (p != null) {
                            foreach (SqlParameter objparam1 in p) {
                                command.Parameters.Add(objparam1);
                            }
                        }
                        try {
                            result = await command.ExecuteNonQueryAsync();
                        } catch (Exception ex) {
                            errormsg = ex.ToString();
                            isError = true;
                            tran.Rollback();
                            throw;
                        }
                        tran.Commit();
                    }
                }
            }
            if (isError) {
                outcome.Success = false;
                outcome.Message = "A query Failed. Please see logs for exact error";
                outcome.Techmessage = "GetDataTable error. Query is[" + strQuery + "] Error:[" + errormsg + "]";
            }
            outcome.Intvar = result;

            return outcome;
        }

        #endregion

        #region Async DataSet Methods


        public async Task<DTReturnClass> GetDataTableAsync(string queryString) {
            return await AsyncDataTableMethods(queryString, null, false);
        }

        public async Task<DTReturnClass> GetDataTableParamsAsync(string queryString, SP_Parameters p) {
            return await AsyncDataTableMethods(queryString, p, false);
        }

        public async Task<DTReturnClass> GetDataTableProcAsync(string procname) {
            return await AsyncDataTableMethods(procname, null, true);
        }

        public async Task<DTReturnClass> GetDataTableProcParamsAsync(string procname, SP_Parameters p) {
            return await AsyncDataTableMethods(procname, p, true);
        }

        private async Task<DTReturnClass> AsyncDataTableMethods(string strQuery, SP_Parameters p, bool isProc) {
            DTReturnClass outcome = new DTReturnClass(true);
            DataTable dt = new DataTable();

            string errormsg = "";
            bool isError = false;
            using (var conn = new SqlConnection(connectionstring)) {
                await conn.OpenAsync();
                using (var command = new SqlCommand(strQuery, conn)) {
                    command.CommandTimeout = commandtimeout;
                    if (isProc) {
                        command.CommandType = CommandType.StoredProcedure;
                    } else {
                        command.CommandType = CommandType.Text;
                    }

                    if (p != null) {
                        foreach (SqlParameter objparam1 in p) {
                            command.Parameters.Add(objparam1);
                        }
                    }

                    try {
                        using (SqlDataAdapter sda = new SqlDataAdapter(command)) {
                            sda.Fill(dt);
                        }

                    } catch (Exception ex) {
                        errormsg = ex.ToString();
                        isError = true;
                    }
                }
            }


            if (isError) {
                outcome.Success = false;
                outcome.Message = "A query Failed. Please see logs for exact error";
                outcome.Techmessage = "GetDataTable error. Query is[" + strQuery + "] Error:[" + errormsg + "]";
            } else {
                if (dt != null) {
                    outcome.Datatable = dt;
                } else {
                    outcome.Success = false;
                    outcome.Message = "A query Failed. Please see logs for exact error";
                    outcome.Techmessage = "GetDataTable error. Datatable is null";
                }
            }

            return outcome;
        }


        #endregion

        #region Async Scalar Methods

        public async Task<ReturnClass> GetStringScalarAsync(string strQuery) {
            ReturnClass outcome = await GetScalarAsyncMethods(strQuery, null, false, ScalarType.String);
            return outcome;
        }
        public async Task<ReturnClass> GetStringScalarParamsAsync(string strQuery, SP_Parameters p) {
            ReturnClass outcome = await GetScalarAsyncMethods(strQuery, p, false, ScalarType.String);
            return outcome;
        }
        public async Task<ReturnClass> GetIntScalarAsync(string strQuery) {
            ReturnClass outcome = await GetScalarAsyncMethods(strQuery, null, false, ScalarType.Int);
            return outcome;
        }
        public async Task<ReturnClass> GetIntScalarParamsAsync(string strQuery, SP_Parameters p) {
            ReturnClass outcome = await GetScalarAsyncMethods(strQuery, p, false, ScalarType.Int);
            return outcome;
        }
        public async Task<ReturnClass> GetLongScalarAsync(string strQuery) {
            ReturnClass outcome = await GetScalarAsyncMethods(strQuery, null, false, ScalarType.Long);
            return outcome;
        }
        public async Task<ReturnClass> GetLongScalarParamsAsync(string strQuery, SP_Parameters p) {
            ReturnClass outcome = await GetScalarAsyncMethods(strQuery, p, false, ScalarType.Long);
            return outcome;
        }
        public async Task<ReturnClass> GetDoubleScalarAsync(string strQuery) {
            ReturnClass outcome = await GetScalarAsyncMethods(strQuery, null, false, ScalarType.Double);
            return outcome;
        }
        public async Task<ReturnClass> GetDoubleScalarParamsAsync(string strQuery, SP_Parameters p) {
            ReturnClass outcome = await GetScalarAsyncMethods(strQuery, p, false, ScalarType.Double);
            return outcome;
        }

        public async Task<ReturnClass> GetStringScalarProcAsync(string strQuery) {
            ReturnClass outcome = await GetScalarAsyncMethods(strQuery, null, true, ScalarType.String);
            return outcome;
        }
        public async Task<ReturnClass> GetStringScalarProcParamsAsync(string strQuery, SP_Parameters p) {
            ReturnClass outcome = await GetScalarAsyncMethods(strQuery, p, true, ScalarType.String);
            return outcome;
        }
        public async Task<ReturnClass> GetIntScalarProcAsync(string strQuery) {
            ReturnClass outcome = await GetScalarAsyncMethods(strQuery, null, true, ScalarType.Int);
            return outcome;
        }
        public async Task<ReturnClass> GetIntScalarProcParamsAsync(string strQuery, SP_Parameters p) {
            ReturnClass outcome = await GetScalarAsyncMethods(strQuery, p, true, ScalarType.Int);
            return outcome;
        }
        public async Task<ReturnClass> GetLongScalarProcAsync(string strQuery) {
            ReturnClass outcome = await GetScalarAsyncMethods(strQuery, null, true, ScalarType.Long);
            return outcome;
        }
        public async Task<ReturnClass> GetLongScalarProcParamsAsync(string strQuery, SP_Parameters p) {
            ReturnClass outcome = await GetScalarAsyncMethods(strQuery, p, true, ScalarType.Long);
            return outcome;
        }
        public async Task<ReturnClass> GetDoubleScalarProcAsync(string strQuery) {
            ReturnClass outcome = await GetScalarAsyncMethods(strQuery, null, true, ScalarType.Double);
            return outcome;
        }
        public async Task<ReturnClass> GetDoubleScalarProcParamsAsync(string strQuery, SP_Parameters p) {
            ReturnClass outcome = await GetScalarAsyncMethods(strQuery, p, true, ScalarType.Double);
            return outcome;
        }

        private enum ScalarType {
            None = 0,
            String = 1,
            Int = 2,
            Long = 3,
            Double = 4
        }
        private async Task<ReturnClass> GetScalarAsyncMethods(string strQuery, SP_Parameters p, bool isProc, ScalarType sctype) {
            ReturnClass outcome = new ReturnClass(true);

            string errormsg = "";
            bool isError = false;
            using (var conn = new SqlConnection(connectionstring)) {
                await conn.OpenAsync();
                using (var command = new SqlCommand(strQuery, conn)) {
                    command.CommandTimeout = commandtimeout;

                    if (isProc) {
                        command.CommandType = CommandType.StoredProcedure;
                    } else {
                        command.CommandType = CommandType.Text;
                    }

                    if (p != null) {
                        foreach (SqlParameter objparam1 in p) {
                            command.Parameters.Add(objparam1);
                        }
                    }

                    try {
                        using (var reader = await command.ExecuteReaderAsync()) {
                            if (await reader.ReadAsync()) {
                                switch (sctype) {
                                    case ScalarType.String:
                                        outcome.Message = reader[0].ToString();
                                        break;
                                    case ScalarType.Int:
                                        outcome.Intvar = (int)reader[0];
                                        break;
                                    case ScalarType.Long:
                                        outcome.Longvar = (long)reader[0];
                                        break;
                                    case ScalarType.Double:
                                        outcome.Doublevar = (double)reader[0];
                                        break;
                                }
                            }
                        }
                    } catch (Exception ex) {
                        errormsg = ex.ToString();
                        isError = true;
                    }
                }
            }

            if (isError) {
                outcome.Success = false;
                outcome.Message = "A query Failed. Please see logs for exact error";
                outcome.Techmessage = "GetDataTable error. Query is[" + strQuery + "] Error:[" + errormsg + "]";
            }

            return outcome;
        }
        #endregion

        #region Sync void methods
        public ReturnClass ExecSql(string strQuery) {
            return ExecSqlProcVoidMethods(strQuery, false, null);
        }

        public ReturnClass ExecSqlParams(string q, SP_Parameters p) {
            return ExecSqlProcVoidMethods(q, false, p);
        }

        public ReturnClass ExecProcVoid(string procname) {
            return ExecSqlProcVoidMethods(procname, true, null);
        }

        public ReturnClass ExecProcVoidParams(string procname, SP_Parameters p) {
            return ExecSqlProcVoidMethods(procname, true, p);
        }

        private ReturnClass ExecSqlProcVoidMethods(string strQuery, bool isProc, SP_Parameters p) {

            ReturnClass outcome = new ReturnClass(true);
            int results = 0;
            try {
                using (var conn = new SqlConnection(connectionstring)) {
                    conn.Open();
                    using (var command = new SqlCommand(strQuery, conn)) {
                        command.CommandTimeout = commandtimeout;

                        if (isProc) {
                            command.CommandType = CommandType.StoredProcedure;
                        } else {
                            command.CommandType = CommandType.Text;
                        }

                        if (p != null) {
                            foreach (SqlParameter objparam1 in p) {
                                command.Parameters.Add(objparam1);
                            }
                        }

                        results = command.ExecuteNonQuery();
                    }
                }
            } catch (Exception ex) {
                outcome.SetFailureMessage("An update query Failed. Please see logs for exact error", "ExecSql error. Query is[" + strQuery + "] Error:[" + ex.Message + "]");
                results = -1;
            }
            outcome.Intvar = results;
            return outcome;

        }

        #endregion

        #region Sync Scalar methods

        public ReturnClass GetStringScalar(string query) {
            return SyncScalarMethods(query, null, false, ScalarType.String);
        }

        public ReturnClass GetStringScalarParams(string query, SP_Parameters p) {
            return SyncScalarMethods(query, p, false, ScalarType.String);
        }

        public ReturnClass GetIntScalar(string query) {
            return SyncScalarMethods(query, null, false, ScalarType.Int);
        }

        public ReturnClass GetIntScalarParams(string query, SP_Parameters p) {
            return SyncScalarMethods(query, p, false, ScalarType.Int);
        }

        public ReturnClass GetLongScalar(string query) {
            return SyncScalarMethods(query, null, false, ScalarType.Long);
        }

        public ReturnClass GetLongScalarParams(string query, SP_Parameters p) {
            return SyncScalarMethods(query, p, false, ScalarType.Long);
        }

        public ReturnClass GetDoubleScalarParams(string query, SP_Parameters p) {
            return SyncScalarMethods(query, p, false, ScalarType.Double);
        }

        public ReturnClass GetLongScalarProcParams(string procname, SP_Parameters p) {
            return SyncScalarMethods(procname, p, true, ScalarType.Long);
        }

        public ReturnClass GetDoubleScalarProcParams(string procname, SP_Parameters p) {
            return SyncScalarMethods(procname, p, true, ScalarType.Double);
        }
        public ReturnClass GetIntScalarProcParams(string procname, SP_Parameters p) {
            return SyncScalarMethods(procname, p, true, ScalarType.Int);
        }
        public ReturnClass ExecProcIntResultParams(string query, SP_Parameters p) {
            return SyncScalarMethods(query, p, true, ScalarType.Int);
        }

        private ReturnClass SyncScalarMethods(string strQuery, SP_Parameters p, bool isProc, ScalarType sctype) {
            ReturnClass outcome = new ReturnClass(true);

            try {
                using (var conn = new SqlConnection(connectionstring)) {
                    conn.Open();
                    using (var command = new SqlCommand(strQuery, conn)) {
                        command.CommandTimeout = commandtimeout;

                        if (isProc) {
                            command.CommandType = CommandType.StoredProcedure;
                        } else {
                            command.CommandType = CommandType.Text;
                        }

                        if (p != null) {
                            foreach (SqlParameter objparam1 in p) {
                                command.Parameters.Add(objparam1);
                            }
                        }

                        try {
                            using (var reader = command.ExecuteReader()) {
                                if (reader.Read()) {
                                    switch (sctype) {
                                        case ScalarType.String:
                                            outcome.Message = reader[0].ToString();
                                            break;
                                        case ScalarType.Int:
                                            outcome.Intvar = (int)reader[0];
                                            break;
                                        case ScalarType.Long:
                                            outcome.Longvar = (long)reader[0];
                                            break;
                                        case ScalarType.Double:
                                            outcome.Doublevar = (double)reader[0];
                                            break;
                                    }
                                }
                            }
                        } catch (Exception ex) {
                            outcome.SetFailureMessage("A query Failed. Please see logs for exact error", "SyncScalarMethods Error Query is[" + strQuery + "] Error:[" + ex.ToString() + "]");
                        }
                    }
                }
            } catch (SqlException ex) {
                outcome.SetFailureMessage("A query Failed. Please see logs for exact error", "SyncScalarMethods Error Query is[" + strQuery + "] Sql Error:[" + ex.ToString() + "]");
            } catch (Exception ex) {
                outcome.SetFailureMessage("A query Failed. Please see logs for exact error", "SyncScalarMethods Error Query is[" + strQuery + "] Error:[" + ex.ToString() + "]");
            }
            return outcome;
        }

        #endregion

        #region Sync DataSet methods


        public DTReturnClass GetDataTable(string queryString) {
            return SyncDatatableMethods(queryString, false, null);
        }

        public DTReturnClass GetDataTableParams(string queryString, SP_Parameters p) {
            return SyncDatatableMethods(queryString, false, p);
        }

        public DTReturnClass GetDataTableProc(string procname) {
            return SyncDatatableMethods(procname, true, null);
        }

        public DTReturnClass GetDataTableProcParams(string procname, SP_Parameters p) {
            return SyncDatatableMethods(procname, true, p);
        }

        public DTReturnClass ExecProcRS(string procname) {
            return SyncDatatableMethods(procname, true, null);
        }

        public DTReturnClass ExecProcRSParams(string procname, SP_Parameters p) {
            return SyncDatatableMethods(procname, true, p);
        }

        private DTReturnClass SyncDatatableMethods(string strQuery, bool isProc, SP_Parameters p) {
            DTReturnClass outcome = new DTReturnClass(true);
            DataTable dt = new DataTable();

            try {
                using (var conn = new SqlConnection(connectionstring)) {
                    conn.Open();
                    using (var command = new SqlCommand(strQuery, conn)) {
                        command.CommandTimeout = commandtimeout;
                        if (isProc) {
                            command.CommandType = CommandType.StoredProcedure;
                        } else {
                            command.CommandType = CommandType.Text;
                        }

                        if (p != null) {
                            foreach (SqlParameter objparam1 in p) {
                                command.Parameters.Add(objparam1);
                            }
                        }

                        try {
                            using (SqlDataAdapter sda = new SqlDataAdapter(command)) {
                                sda.Fill(dt);
                            }

                            outcome.Datatable = dt;

                        } catch (Exception ex) {
                            outcome.Success = false;
                            outcome.Message = "A query Failed. Please see logs for exact error";
                            outcome.Techmessage = "SyncDatatableMethods error. Query is[" + strQuery + "] Error:[" + ex.ToString() + "]";
                        }
                    }
                }
            } catch (SqlException ex) {
                outcome.Success = false;
                outcome.Message = "A query Failed. Please see logs for exact error";
                outcome.Techmessage = "SyncDatatableMethods error. Query is[" + strQuery + "] Error:[" + ex.ToString() + "]";
            } catch (Exception ex) {
                outcome.Success = false;
                outcome.Message = "A query Failed. Please see logs for exact error";
                outcome.Techmessage = "SyncDatatableMethods error. Query is[" + strQuery + "] Error:[" + ex.ToString() + "]";
            }

            return outcome;
        }
        #endregion

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                // dispose managed resources
                //if (conn.State != ConnectionState.Closed) {
                //    conn.Close();
                //    //conn.Dispose();
                //}
            }
            // free native resources
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


    }


    public class SP_Parameters : IEnumerable {
        private List<SqlParameter> Items = null;
        private SqlParameter sqlparam;

        public SP_Parameters() {
            Items = new List<SqlParameter>();
        }
        public void Add(SqlParameter para) {
            Items.Add(para);
        }
        public void Add(string parametername, SqlDbType dbType, int paramsize, ParameterDirection direction, object paramvalue) {
            sqlparam = new SqlParameter(parametername, dbType);
            sqlparam.Direction = direction;
            if (paramsize != 0)
                sqlparam.Size = paramsize;
            if (paramvalue != null)
                sqlparam.Value = paramvalue;
            Items.Add(sqlparam);
        }

        public IEnumerator GetEnumerator() {
            return this.Items.GetEnumerator();
        }
    }

}
