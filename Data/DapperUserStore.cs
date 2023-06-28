using Dapper;
using Microsoft.AspNetCore.Identity;
using System.Data;
using System.Data.SqlClient;

namespace IdentityTest.Data.Identity
{
    public class DapperUserStore : IUserStore<IdentityUser>, IUserPasswordStore<IdentityUser>
    {
        private readonly IDbConnection _connection;

        public DapperUserStore(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IdentityResult> CreateAsync(
            IdentityUser user,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            var existingUser = await _connection.QuerySingleOrDefaultAsync<IdentityUser>(
                "SELECT * FROM AspNetUsers WHERE NormalizedUserName = @NormalizedUserName OR NormalizedEmail = @NormalizedEmail",
                new
                {
                    NormalizedUserName = user.NormalizedUserName,
                    NormalizedEmail = user.NormalizedEmail
                }
            );

            if (existingUser != null)
            {
                // You may want to differentiate between a username and email collision,
                // this is just a simple example that combines them.
                return IdentityResult.Failed(
                    new IdentityError
                    {
                        Code = nameof(CreateAsync),
                        Description = "A user with this username or email already exists."
                    }
                );
            }

            if (string.IsNullOrEmpty(user.UserName))
            {
                throw new ArgumentException("Username cannot be null or empty");
            }
            if (string.IsNullOrEmpty(user.Email))
            {
                throw new ArgumentException("Email cannot be null or empty");
            }

            try
            {
                const string sql =
                    "INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount) VALUES (@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, @EmailConfirmed, @PasswordHash, @SecurityStamp, @ConcurrencyStamp, @PhoneNumber, @PhoneNumberConfirmed, @TwoFactorEnabled, @LockoutEnd, @LockoutEnabled, @AccessFailedCount)";

                await ((SqlConnection)_connection).OpenAsync(cancellationToken);

                int rowsAffected = await _connection.ExecuteAsync(sql, user);
                if (rowsAffected != 1)
                {
                    // This should not occur under normal conditions and likely indicates an issue with the database.
                    throw new Exception("Could not create user");
                }
            }
            catch (Exception ex)
            {
                return IdentityResult.Failed(
                    new IdentityError
                    {
                        Code = nameof(CreateAsync),
                        Description = $"An error occurred while creating a user: {ex.Message}"
                    }
                );
            }

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(
            IdentityUser user,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            // Check if user actually exists in the database before attempting deletion
            var existingUser = await _connection.QuerySingleOrDefaultAsync<IdentityUser>(
                "SELECT * FROM AspNetUsers WHERE Id = @Id",
                new { Id = user.Id }
            );

            if (existingUser == null)
            {
                // Return an error if the user does not exist
                return IdentityResult.Failed(
                    new IdentityError
                    {
                        Code = "UserNotExist",
                        Description = $"User with id {user.Id} does not exist."
                    }
                );
            }

            try
            {
                var sql = "DELETE FROM AspNetUsers WHERE Id = @Id";
                await _connection.ExecuteAsync(sql, new { Id = user.Id });

                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                // Log the exception somewhere
                // Then return an error indicating the operation could not be completed
                return IdentityResult.Failed(
                    new IdentityError
                    {
                        Code = "DeleteFailed",
                        Description = $"Could not delete user with id {user.Id}: {ex.Message}"
                    }
                );
            }
        }

        public void Dispose()
        {
            try
            {
                // If _connection is not null and it's still open, close it
                if (_connection != null && _connection.State == ConnectionState.Open)
                {
                    _connection.Close();
                }
            }
            catch (Exception ex)
            {
                // Log the exception or throw a more specific exception depending on your application's needs
                throw new Exception("An error occurred while disposing the connection.", ex);
            }
            finally
            {
                // Dispose the _connection object
                _connection?.Dispose();
            }
        }

        public async Task<IdentityUser> FindByIdAsync(
            string userId,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (!Guid.TryParse(userId, out var guidUserId))
            {
                throw new ArgumentException("Invalid user id format.", nameof(userId));
            }

            var sql = "SELECT * FROM AspNetUsers WHERE Id = @Id";

            IdentityUser? user = null;
            try
            {
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);
                user = await _connection.QuerySingleOrDefaultAsync<IdentityUser>(
                    sql,
                    new { Id = userId }
                );
            }
            catch (SqlException exception)
            {
                // Log exception here or handle it as per your requirements.
                throw new Exception(
                    "An error occurred while retrieving the user from the database.",
                    exception
                );
            }
            finally
            {
                await ((SqlConnection)_connection).CloseAsync();
            }

            return user;
        }

        public async Task<IdentityUser> FindByNameAsync(
            string normalizedUserName,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(normalizedUserName))
            {
                throw new ArgumentException(
                    "The normalized user name cannot be null or whitespace.",
                    nameof(normalizedUserName)
                );
            }

            var sql = "SELECT * FROM AspNetUsers WHERE NormalizedUserName = @NormalizedUserName";

            IdentityUser? user = null;

            try
            {
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);
                user = await _connection.QuerySingleOrDefaultAsync<IdentityUser>(
                    sql,
                    new { NormalizedUserName = normalizedUserName }
                );
            }
            catch (SqlException exception)
            {
                // Log exception here or handle it as per your requirements.
                // You might want to re-throw the exception after logging it.
                throw;
            }
            finally
            {
                await ((SqlConnection)_connection).CloseAsync();
            }

            return user;
        }

        public async Task<IEnumerable<IdentityUser>> GetAllUsersAsync()
        {
            var result = await _connection.QueryAsync<IdentityUser>("SELECT * FROM AspNetUsers");
            return result;
        }

        public async Task<string> GetNormalizedUserNameAsync(
            IdentityUser user,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var sql = "SELECT NormalizedUserName FROM AspNetUsers WHERE Id = @Id";

            string? normalizedUserName = null;

            try
            {
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);
                normalizedUserName = await _connection.QuerySingleOrDefaultAsync<string>(
                    sql,
                    new { Id = user.Id }
                );
            }
            catch (Exception ex)
            {
                // Log exception here or handle it as per your requirements.
                // You might want to re-throw the exception after logging it.
                throw new InvalidOperationException("Failed to get normalized username", ex);
            }
            finally
            {
                await ((SqlConnection)_connection).CloseAsync();
            }

            return normalizedUserName;
        }

        public async Task<string> GetPasswordHashAsync(
            IdentityUser user,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            try
            {
                var sql = "SELECT PasswordHash FROM AspNetUsers WHERE Id = @Id";
                return await _connection.QuerySingleOrDefaultAsync<string>(
                    sql,
                    new { Id = user.Id }
                );
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get password hash", ex);
            }
        }

        public Task<string> GetUserIdAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            try
            {
                return Task.FromResult(user.Id.ToString());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get user Id", ex);
            }
        }

        public Task<string> GetUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            try
            {
                return Task.FromResult(user.UserName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get UserName", ex);
            }
        }

        public async Task<bool> HasPasswordAsync(
            IdentityUser user,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            try
            {
                var sql = "SELECT PasswordHash FROM AspNetUsers WHERE Id = @Id";
                var passwordHash = await _connection.QuerySingleOrDefaultAsync<string>(
                    sql,
                    new { Id = user.Id }
                );
                return !string.IsNullOrEmpty(passwordHash);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to check if user has password", ex);
            }
        }

        public Task SetNormalizedUserNameAsync(
            IdentityUser user,
            string normalizedName,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (normalizedName == null)
            {
                throw new ArgumentNullException(nameof(normalizedName));
            }

            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public async Task SetPasswordHashAsync(
            IdentityUser user,
            string passwordHash,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (passwordHash == null)
            {
                throw new ArgumentNullException(nameof(passwordHash));
            }

            var sql = "UPDATE AspNetUsers SET PasswordHash = @PasswordHash WHERE Id = @Id";

            try
            {
                await _connection.ExecuteAsync(
                    sql,
                    new { PasswordHash = passwordHash, Id = user.Id }
                );
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Failed to set password hash", ex);
            }

            // Also update the in-memory user object
            user.PasswordHash = passwordHash;
        }

        public async Task SetUserNameAsync(
            IdentityUser user,
            string userName,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException(
                    "Username cannot be null or white space.",
                    nameof(userName)
                );
            }

            if (userName.Length > 256)
            {
                throw new ArgumentException(
                    "Username cannot be more than 256 characters.",
                    nameof(userName)
                );
            }

            var sql = "UPDATE AspNetUsers SET UserName = @UserName WHERE Id = @Id";

            try
            {
                await _connection.ExecuteAsync(sql, new { UserName = userName, Id = user.Id });
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Failed to set username", ex);
            }

            // Also update the in-memory user object
            user.UserName = userName;
        }

        public async Task<IdentityResult> UpdateAsync(
            IdentityUser user,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            // Check if the user exists
            var existingUser = await _connection.QuerySingleOrDefaultAsync<IdentityUser>(
                "SELECT * FROM AspNetUsers WHERE Id = @Id",
                new { Id = user.Id }
            );

            if (existingUser == null)
            {
                return IdentityResult.Failed(
                    new IdentityError { Description = $"User with ID {user.Id} does not exist." }
                );
            }

            try
            {
                var sql =
                    @"UPDATE AspNetUsers SET 
                    UserName = @UserName,
                    NormalizedUserName = @NormalizedUserName,
                    Email = @Email,
                    NormalizedEmail = @NormalizedEmail,
                    EmailConfirmed = @EmailConfirmed,
                    PasswordHash = @PasswordHash,
                    SecurityStamp = @SecurityStamp,
                    ConcurrencyStamp = @ConcurrencyStamp,
                    PhoneNumber = @PhoneNumber,
                    PhoneNumberConfirmed = @PhoneNumberConfirmed,
                    TwoFactorEnabled = @TwoFactorEnabled,
                    LockoutEnd = @LockoutEnd,
                    LockoutEnabled = @LockoutEnabled,
                    AccessFailedCount = @AccessFailedCount
                WHERE Id = @Id";

                await _connection.ExecuteAsync(sql, user);
                return IdentityResult.Success;
            }
            catch (SqlException ex)
            {
                return IdentityResult.Failed(
                    new IdentityError
                    {
                        Description =
                            $"An error occurred while updating the user with ID {user.Id}."
                    }
                );
            }
        }
    }
}
