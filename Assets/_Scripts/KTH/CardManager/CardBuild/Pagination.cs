using System;
using System.Collections.Generic;

public class Pagination
{
    public int CurrentPage { get; private set; } = 1;
    public int ItemsPerPage { get; private set; } = 5;
    public int TotalPages { get; private set; } = 1;

    public void Setup(int totalItemCount, int itemsPerPage)
    {
        ItemsPerPage = Math.Max(1, itemsPerPage);
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalItemCount / ItemsPerPage));

        // 카드가 빠져서 총 페이지 수가 줄어들었을 때 존재하지 않는 페이지를 가리키지 않도록 보정
        if (CurrentPage > TotalPages)
        {
            CurrentPage = TotalPages;
        }
        if (CurrentPage < 1)
        {
            CurrentPage = 1;
        }
    }

    public bool NextPage()
    {
        if (CurrentPage >= TotalPages) return false;
        CurrentPage++;
        return true;
    }

    public bool PrevPage()
    {
        if (CurrentPage <= 1) return false;
        CurrentPage--;
        return true;
    }
}

