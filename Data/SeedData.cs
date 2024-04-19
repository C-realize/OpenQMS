/*
This file is part of the OpenQMS.net project (https://github.com/C-realize/OpenQMS).
Copyright (C) 2022-2024  C-realize IT Services SRL (https://www.c-realize.com)

This program is offered under a commercial and under the AGPL license.
For commercial licensing, contact us at https://www.c-realize.com/contact.  For AGPL licensing, see below.

AGPL:
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see http://www.gnu.org/licenses/.
*/

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenQMS.Authorization;
using OpenQMS.Models;

namespace OpenQMS.Data
{
    public class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider, string testUserPw = "")
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Password is set with the following:
                // dotnet user-secrets set SeedUserPW <pw>
                // The admin user can do anything

                var adminID = await EnsureUser(serviceProvider, testUserPw, "Administrator", "admin@c-realize.com");
                var adminDescription = "Administrators have access to all features.";
                await EnsureRole(serviceProvider, adminID, Constants.AdministratorsRole, adminDescription);

                var managerID = await EnsureUser(serviceProvider, testUserPw, "Manager", "manager@c-realize.com");
                var managerDescription = "Managers can view and approve documents. They can also manage trainings.";
                await EnsureRole(serviceProvider, managerID, Constants.ManagersRole, managerDescription);

                SeedDB(context, testUserPw);
            }
        }

        private static async Task<int> EnsureUser(IServiceProvider serviceProvider,
                                            string testUserPw, string UserName, string Email)
        {
            var userManager = serviceProvider.GetService<UserManager<AppUser>>();

            var user = await userManager.FindByNameAsync(UserName);
            if (user == null)
            {
                user = new AppUser
                {
                    FirstName = UserName,
                    LastName = UserName,
                    UserName = UserName,
                    Email = Email,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(user, testUserPw);
            }

            if (user == null)
            {
                throw new Exception("The password is probably not strong enough!");
            }

            return user.Id;
        }

        private static async Task<IdentityResult> EnsureRole(IServiceProvider serviceProvider,
                                                                      int uid, string role, string roleDescription)
        {
            var roleManager = serviceProvider.GetService<RoleManager<AppRole>>();

            if (roleManager == null)
            {
                throw new Exception("roleManager null");
            }

            IdentityResult IR;
            if (!await roleManager.RoleExistsAsync(role))
            {
                IR = await roleManager.CreateAsync
                (
                    new AppRole 
                    { 
                        Name = role,
                        Description = roleDescription
                    }
                );
            }

            var userManager = serviceProvider.GetService<UserManager<AppUser>>();

            //if (userManager == null)
            //{
            //    throw new Exception("userManager is null");
            //}

            var user = await userManager.FindByIdAsync(uid.ToString());

            if (user == null)
            {
                throw new Exception("The testUserPw password was probably not strong enough!");
            }

            IR = await userManager.AddToRoleAsync(user, role);

            return IR;
        }

        public static void SeedDB(ApplicationDbContext context, string adminID)
        {
            if (context.AppDocument.Any())
            {
                return;   // DB has been seeded
            }

            context.SaveChanges();
        }
    }
}
