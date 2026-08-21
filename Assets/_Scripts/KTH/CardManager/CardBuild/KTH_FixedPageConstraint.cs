public interface IPageConstraint
{
    bool CanAddToPage(int itemIndex, int targetPage);
}

public class KTH_FixedPageConstraint : IPageConstraint
{
    private int _itemsPerPage = 5;

    public void Setup(int itemsPerPage)
    {
        _itemsPerPage = itemsPerPage;
    }

    public bool CanAddToPage(int itemIndex, int targetPage)
    {
        if (_itemsPerPage <= 0) return false;
        int assignedPage = (itemIndex / _itemsPerPage) + 1;
        return assignedPage == targetPage;
    }
}