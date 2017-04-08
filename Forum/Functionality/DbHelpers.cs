using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Forum.Functionality
{
    public class DbHelpers
    {        
        public static string connection = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;            
        public static string command = null;
        public static string parameterName = null;
        public static string methodName = null;       

        //looking user and mail in db in registration process to avoid the same
        public static string ReturnString(string str)
        {
            string strOut = null;
            using (SqlConnection ConnectionDB = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(command, ConnectionDB))
            {
                cmd.Parameters.AddWithValue(parameterName, str);
                ConnectionDB.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            //accon
                            if (methodName == "FindEmail")
                            {
                                strOut = reader["Email"].ToString();
                            }
                            else if (methodName == "FindUserName" || methodName == "FindUserNameById")
                            {
                                strOut = reader["UserName"].ToString();
                            }
                            else if (methodName == "FindUserRole")
                            {
                                strOut = reader["Name"].ToString();
                            }
                            else if (methodName == "FindUserId")
                            {
                                strOut = reader["UserId"].ToString();
                            }
                            //forcon
                            else if (methodName == "FindCategoty")
                            {
                                strOut = reader["Name"].ToString();
                            }
                            else if (methodName == "FindTag")
                            {
                                strOut = reader["Name"].ToString();
                            }
                        }
                        ConnectionDB.Close();
                    }
                    return strOut;
                }
            }
        }

        //AcCon
        public string FindEmail(string email)
        {
            command = "SELECT Email AS Email FROM AspNetUsers WHERE Email = @Email";
            parameterName = "@Email";
            methodName = "FindEmail";
            return ReturnString(email);
        }

        public string FindUserName(string username)
        {
            command = "SELECT UserName AS UserName FROM AspNetUsers WHERE UserName = @UserName";
            parameterName = "@UserName";
            methodName = "FindUserName";
            return ReturnString(username);
        }

        public string CheckUserRole(string username)
        {
            command =
                "SELECT Name FROM AspNetRoles " +
                "INNER JOIN AspNetUserRoles ON AspNetUserRoles.RoleId = AspNetRoles.Id " +
                "INNER JOIN AspNetUsers ON AspNetUsers.Id =  AspNetUserRoles.UserId " +
                "WHERE UserName = @UserName";
            parameterName = "@UserName";
            methodName = "FindUserRole";
            return ReturnString(username);
        }

        //ForCon
        public string FindCategoty(string categoty)
        {
            command = "SELECT Name FROM Category WHERE Name = @Name";
            parameterName = "@Name";
            methodName = "FindCategoty";
            return ReturnString(categoty);
        }
        
        public string FindTag(string tag)
        {
            command = "SELECT Name FROM Tag WHERE Name = @Name";
            parameterName = "@Name";
            methodName = "FindTag";
            return ReturnString(tag);
        }

        //Accon external log
        public string FindUserNameById(string userid)
        {
            command = "SELECT UserName AS UserName FROM AspNetUsers WHERE Id = @Id";
            parameterName = "@Id";
            methodName = "FindUserNameById";
            return ReturnString(userid);
        }

        public string FindUserId(string userprokey)
        {
            command = "SELECT UserId AS UserId FROM AspNetUserLogins WHERE ProviderKey = @ProviderKey";
            parameterName = "@ProviderKey";
            methodName = "FindUserId";
            return ReturnString(userprokey);
        }        

        //return bool for email conform to allow login
        public bool ReturnBool(string str)
        {
            bool econfOut = false;
            string res = null;
            using (SqlConnection ConnectionDB = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(command, ConnectionDB))
            {
                cmd.Parameters.AddWithValue(parameterName, str);
                ConnectionDB.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            res = reader["EmailConfirmed"].ToString();
                            if (res == "False")
                            {
                                econfOut = false;
                            }
                            else
                            {
                                econfOut = true;
                            }
                        }
                        ConnectionDB.Close();
                    }
                    return econfOut;
                }
            }
        }
        //AcCon
        public bool EmailConfirmation(string username)
        {
            command = "SELECT EmailConfirmed AS EmailConfirmed FROM AspNetUsers WHERE UserName = @UserName";
            parameterName = "@UserName";
            return ReturnBool(username);
        }

        public int UpdateDatabase(string username)
        {
            using (SqlConnection ConnectionDB = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(command, ConnectionDB))
            {
                cmd.Parameters.AddWithValue(parameterName, username);
                ConnectionDB.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        //Updating last login date in db
        public int UpdateLastLoginDate(string username)
        {
            command = "UPDATE AspNetUsers SET LastLoginDate = '" + DateTime.Now + "' WHERE UserName = @UserName";
            parameterName = "@UserName";
            return UpdateDatabase(username);
        }

        public int UpdateOnlineStatus(string username)
        {
            command = "UPDATE AspNetUsers SET OnlineStatus = 'true' WHERE UserName = @UserName";
            parameterName = "@UserName";
            return UpdateDatabase(username);
        }

        public int UpdateOfflineStatus(string username)
        {
            command = "UPDATE AspNetUsers SET OnlineStatus = 'false' WHERE UserName = @UserName";
            parameterName = "@UserName";
            return UpdateDatabase(username);
        }
        
    }
}