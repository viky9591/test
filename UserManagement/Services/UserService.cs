using UserManagement.Core.Entities;
using UserManagement.Helpers;
using UserManagement.Infrastucture;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Core.ViewModels;

namespace UserManagement.Services
{

    public interface IUserService {

        List<User> GetAll();
        User Create(User user,string password);

        User Authenticate(string username, string password);

        User GetUserById(int id);

         updateViewModel EditUser(updateViewModel user);
        void  DeleteUser(User user);
  
    }

    public class UserService : IUserService
    {
        private ApplicationDbContext _dbContext;

        public UserService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
    
        public User Authenticate(string username, string password)
        {
            // checking user and password null or not given by user
            if(string.IsNullOrEmpty(password)||string.IsNullOrEmpty(username))
                return null;

            var user = _dbContext.Users.SingleOrDefault(x => x.Username == username);

        //checking user exit or not 

            if (user == null)  
              return null;

            //checking password exit or not 
            if (!VerifyPasswordhash(password, user.PasswordHash, user.PasswordSalt))
                return null;

            //authentication successfully!

            return user;

        }

        public User Create(User user, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new CustomException("Password is Required");
            }
            if (_dbContext.Users.Any(x => x.Username == user.Username))
            {
                throw new CustomException($"Username {user.Username} is already taken");
            }

            byte[] passwordHash, passwordSalt;

            CreatePasswordHash(password, out passwordHash, out passwordSalt);

            user.PasswordSalt=passwordSalt;
            user.PasswordHash = passwordHash;
            user.CreatedDate=DateTime.Now;

            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();

            return user;
        }

        public User GetUserById(int id)
        {
            return _dbContext.Users.Find(id);
        }


        private static void CreatePasswordHash(string password,out byte[] passwordhash,out byte[] passwordSalt)
        {
            if (password == null) throw new ArgumentNullException("Password");
            if(string.IsNullOrWhiteSpace(password)) throw new ArgumentNullException("value cannot be empty");

            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordhash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));            }
        }

        private static bool VerifyPasswordhash(string password, byte[] storeHash, byte[] storeSalt)
        {
            if (password == null) throw new ArgumentNullException("Password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentNullException("value cannot be empty");
            if (storeHash.Length !=64) throw new ArgumentNullException("invalid length of password hash ");
            if (storeSalt.Length != 128) throw new ArgumentNullException("invalid length of  password salt ");

            using (var hmac = new System.Security.Cryptography.HMACSHA512(storeSalt))
            {
                var computedHash=hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != storeHash[i]) return false;
                } 
            }
            return true;
        }

        public List<User> GetAll()
        {
            return _dbContext.Users.ToList();
        }

        public void DeleteUser(User user)
        {
            _dbContext.Users.Remove(user);
            _dbContext.SaveChanges();
            
        }

        public updateViewModel EditUser(updateViewModel user)
        {
            var ExitingUser = _dbContext.Users.Find(user.ID);
            if (ExitingUser != null)
            {
                ExitingUser.Username = user.Username;
                ExitingUser.Email = user.Email;
                ExitingUser.MobileNumber = user.MobileNumber;
                _dbContext.Users.Update(ExitingUser);
                _dbContext.SaveChanges();
            }
            return user;
        }

      
    }
}
