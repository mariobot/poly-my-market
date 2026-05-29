using PolyMyMarket.Models;

namespace PolyMyMarket.Web.Services;

public class UserSessionService
{
    private User? _currentUser;
    public event Action? OnUserChanged;

    public User? CurrentUser
    {
        get => _currentUser;
        private set
        {
            _currentUser = value;
            OnUserChanged?.Invoke();
        }
    }

    public void SetCurrentUser(User user)
    {
        CurrentUser = user;
    }

    public void ClearCurrentUser()
    {
        CurrentUser = null;
    }

    public bool HasCurrentUser => CurrentUser != null;
}
