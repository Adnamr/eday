using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Security;
using EdayRoom.Models;

namespace EdayRoom.Security
{
    public class EdayRoleProvider : RoleProvider
    {
        public override string ApplicationName
        {
            get { return "EdayRoom"; }
            set { }
        }

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            var db = new edayRoomEntities();
            user user = db.users.SingleOrDefault(u => u.username == username);
            if (user == null)
                return false;
            else
            {
                if (user.admin)
                    return true;
                switch (roleName)
                {
                    case "dahsboard":
                        return user.dashboard;
                    case "participacion":
                        return user.participacion;
                    case "movilizacion":
                        return user.movilizacion;
                    case "exitpolls":
                        return user.exitpolls;
                    case "quickcount":
                        return user.quickcount;
                    case "totalizacion":
                        return user.totalizacion;
                    case "alertas":
                        return user.alertas;
                    case "supervisor":
                        return user.supervisor;
                    case "leader":
                        return user.leader;
                    case "participacion-lider":
                        return (user.participacion) && (user.leader);
                    case "movilizacion-lider":
                        return (user.movilizacion) && (user.leader);
                    case "exitpolls-lider":
                        return (user.exitpolls) && (user.leader);
                    case "quickcount-lider":
                        return (user.quickcount) && (user.leader);
                    case "totalizacion-lider":
                        return (user.totalizacion) && (user.leader);
                }
                return false;
            }
        }


        public override string[] GetRolesForUser(string username)
        {
            var roles = new List<string>();
            var db = new edayRoomEntities();
            user user = db.users.SingleOrDefault(u => u.username == username);
            if (user == null)
                return roles.ToArray();

            if ((user.admin) || (user.leader))
            {
                roles.Add("leader");
            }

            if ((user.admin) || (user.participacion))
            {
                roles.Add("participacion");
                if ((user.admin) || (user.leader))
                {
                    roles.Add("participacion-lider");
                }
            }

            if ((user.admin) || (user.movilizacion))
            {
                roles.Add("movilizacion");
                if ((user.admin) || (user.leader))
                {
                    roles.Add("movilizacion-lider");
                }
            }

            if ((user.admin) || (user.exitpolls))
            {
                roles.Add("exitpolls");
                if ((user.admin) || (user.leader))

                {
                    roles.Add("exitpolls-lider");
                }
            }

            if ((user.admin) || (user.quickcount))
            {
                roles.Add("quickcount");
                if ((user.admin) || (user.leader))

                {
                    roles.Add("quickcount-lider");
                }
            }

            if ((user.admin) || (user.totalizacion))
            {
                roles.Add("totalizacion");
                if ((user.admin) || (user.leader))

                {
                    roles.Add("totalizacion-lider");
                }
            }
            if ((user.admin) || (user.dashboard))
            {
                roles.Add("dashboard");
                if ((user.admin) || (user.leader))
                {
                    roles.Add("dashboard-lider");
                }
            }
            if ((user.admin) || user.alertas)
            {
                roles.Add("alertas");
            }
            if ((user.admin) || user.supervisor)
            {
                roles.Add("supervisor");
            }
            if (user.admin)
            {
                roles.Add("admin");
            }
            return roles.ToArray();
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }


        public override void CreateRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            throw new NotImplementedException();
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override bool RoleExists(string roleName)
        {
            throw new NotImplementedException();
        }
    }
}