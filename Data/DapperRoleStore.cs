using Dapper;
using Microsoft.AspNetCore.Identity;
using System.Data;
using System.Data.SqlClient;

namespace IdentityTest.Data.Identity
{
    public class DapperRoleStore : IRoleStore<IdentityRole>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<DapperRoleStore> _logger;

        public DapperRoleStore(IDbConnection connection, ILogger<DapperRoleStore> logger)
        {
            _connection = connection;
            _logger = logger;
            _logger.LogInformation("DapperRoleStore initialized");
        }

        public async Task<IEnumerable<IdentityRole>> GetAllRolesAsync()
        {
            var result = await _connection.QueryAsync<IdentityRole>("SELECT * FROM AspNetRoles");
            return result;
        }

        public async Task<IdentityResult> CreateAsync(
            IdentityRole role,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("DapperRoleStore.CreateAsync() called");

            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            _logger.LogInformation(role.Name);

            // Check for role Name
            if (string.IsNullOrWhiteSpace(role.Name) || role.Name.Length > 256)
            {
                return IdentityResult.Failed(
                    new IdentityError
                    {
                        Code = "InvalidRoleName",
                        Description = "Role name is either empty, or length is greater than 256."
                    }
                );
            }

            var sql =
                @"INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (@Id, @Name, @NormalizedName, @ConcurrencyStamp)";

            try
            {
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);
                await _connection.ExecuteAsync(sql, role);
                return IdentityResult.Success;
            }
            catch (SqlException ex)
            {
                // Log the exception here
                // ....

                // Then, throw a new exception that doesn't contain sensitive details
                throw new Exception("An error occurred while creating the role.", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await ((SqlConnection)_connection).CloseAsync();
            }
        }

        public async Task<IdentityResult> DeleteAsync(
            IdentityRole role,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            try
            {
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);

                var sql = "DELETE FROM AspNetRoles WHERE Id = @Id";
                int rowsDeleted = await _connection.ExecuteAsync(sql, new { Id = role.Id });

                if (rowsDeleted > 0)
                {
                    return IdentityResult.Success;
                }
                else
                {
                    return IdentityResult.Failed(
                        new IdentityError { Code = "RoleNotFound", Description = "Role not found." }
                    );
                }
            }
            catch (SqlException exception)
            {
                // Log exception or do something else with it if necessary
                return IdentityResult.Failed(
                    new IdentityError
                    {
                        Code = "DatabaseError",
                        Description = "A database error occurred."
                    }
                );
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await ((SqlConnection)_connection).CloseAsync();
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

        public async Task<IdentityRole> FindByIdAsync(
            string roleId,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(roleId))
            {
                throw new ArgumentException(
                    "Role ID cannot be null or whitespace.",
                    nameof(roleId)
                );
            }

            try
            {
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);

                var query = "SELECT * FROM AspNetRoles WHERE Id = @RoleId";
                var role = await _connection.QuerySingleOrDefaultAsync<IdentityRole>(
                    query,
                    new { RoleId = roleId }
                );

                return role;
            }
            catch (SqlException ex)
            {
                // Handle or log SQL exception as needed...
                throw new ApplicationException("An error occurred while finding the role.", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await ((SqlConnection)_connection).CloseAsync();
            }
        }

        public async Task<IdentityRole> FindByNameAsync(
            string normalizedRoleName,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("FindByNameAsync called");

            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(normalizedRoleName))
            {
                throw new ArgumentNullException(nameof(normalizedRoleName));
            }

            try
            {
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);
                var sql = "SELECT * FROM AspNetRoles WHERE NormalizedName = @NormalizedRoleName";
                var role = await _connection.QuerySingleOrDefaultAsync<IdentityRole>(
                    sql,
                    new { NormalizedRoleName = normalizedRoleName }
                );

                return role;
            }
            catch (SqlException ex)
            {
                // Log the exception or throw a more specific exception depending on your application's needs
                throw new Exception(
                    "An error occurred while attempting to find the role by name.",
                    ex
                );
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await ((SqlConnection)_connection).CloseAsync();
            }
        }

        public async Task<string> GetNormalizedRoleNameAsync(
            IdentityRole role,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            try
            {
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);
                var sql = "SELECT NormalizedName FROM AspNetRoles WHERE Id = @Id";
                var normalizedRoleName = await _connection.QuerySingleOrDefaultAsync<string>(
                    sql,
                    new { Id = role.Id }
                );

                return normalizedRoleName;
            }
            catch (SqlException ex)
            {
                // Log the exception or throw a more specific exception depending on your application's needs
                throw new Exception(
                    "An error occurred while attempting to get the normalized role name.",
                    ex
                );
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await ((SqlConnection)_connection).CloseAsync();
            }
        }

        public async Task<string> GetRoleIdAsync(
            IdentityRole role,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            try
            {
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);
                var sql = "SELECT Id FROM AspNetRoles WHERE NormalizedName = @NormalizedName";
                var roleId = await _connection.QuerySingleOrDefaultAsync<string>(
                    sql,
                    new { NormalizedName = role.NormalizedName }
                );

                return roleId;
            }
            catch (SqlException ex)
            {
                // Log the exception or throw a more specific exception depending on your application's needs
                throw new Exception("An error occurred while attempting to get the role ID.", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await ((SqlConnection)_connection).CloseAsync();
            }
        }

        public async Task<string> GetRoleNameAsync(
            IdentityRole role,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            try
            {
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);
                var sql = "SELECT Name FROM AspNetRoles WHERE Id = @Id";
                var roleName = await _connection.QuerySingleOrDefaultAsync<string>(
                    sql,
                    new { Id = role.Id }
                );

                return roleName;
            }
            catch (SqlException ex)
            {
                // Log the exception or throw a more specific exception depending on your application's needs
                throw new Exception("An error occurred while attempting to get the role name.", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await ((SqlConnection)_connection).CloseAsync();
            }
        }

        public async Task SetNormalizedRoleNameAsync(
            IdentityRole role,
            string normalizedName,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                throw new ArgumentException(
                    "Value cannot be null or whitespace.",
                    nameof(normalizedName)
                );
            }

            try
            {
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);

                var sql = "UPDATE AspNetRoles SET NormalizedName = @NormalizedName WHERE Id = @Id";

                int rowsAffected = await _connection.ExecuteAsync(
                    sql,
                    new { NormalizedName = normalizedName, Id = role.Id }
                );

                if (rowsAffected == 0)
                {
                    throw new Exception($"No role found with ID {role.Id}");
                }
            }
            catch (SqlException ex)
            {
                // Log the exception or throw a more specific exception depending on your application's needs
                throw new Exception(
                    "An error occurred while attempting to set the normalized role name.",
                    ex
                );
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await ((SqlConnection)_connection).CloseAsync();
            }
        }

        public async Task SetRoleNameAsync(
            IdentityRole role,
            string roleName,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException(
                    "Value cannot be null or whitespace.",
                    nameof(roleName)
                );
            }

            try
            {
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);

                var sql = "UPDATE AspNetRoles SET Name = @Name WHERE Id = @Id";

                int rowsAffected = await _connection.ExecuteAsync(
                    sql,
                    new { Name = roleName, Id = role.Id }
                );

                if (rowsAffected == 0)
                {
                    throw new Exception($"No role found with ID {role.Id}");
                }
            }
            catch (SqlException ex)
            {
                // Log the exception or throw a more specific exception depending on your application's needs
                throw new Exception("An error occurred while attempting to set the role name.", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await ((SqlConnection)_connection).CloseAsync();
            }
        }

        public async Task<IdentityResult> UpdateAsync(
            IdentityRole role,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            try
            {
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);

                var sql =
                    "UPDATE AspNetRoles SET Name = @Name, NormalizedName = @NormalizedName WHERE Id = @Id";

                int rowsAffected = await _connection.ExecuteAsync(
                    sql,
                    new
                    {
                        role.Name,
                        role.NormalizedName,
                        role.Id
                    }
                );

                if (rowsAffected == 0)
                {
                    throw new Exception($"No role found with ID {role.Id}");
                }

                return IdentityResult.Success;
            }
            catch (SqlException ex)
            {
                // Log the exception or throw a more specific exception depending on your application's needs
                throw new Exception("An error occurred while attempting to update the role.", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await ((SqlConnection)_connection).CloseAsync();
            }
        }
    }
}
