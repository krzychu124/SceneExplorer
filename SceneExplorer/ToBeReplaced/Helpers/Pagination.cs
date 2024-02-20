using System.Collections.Generic;
using System.Collections.Generic;

namespace SceneExplorer.ToBeReplaced.Helpers
{

    public struct Pagination<T>
    {
        private int _itemPerPage;
        public int CurrentPage { get; private set; }
        public int ItemCount => Data.Count;

        public int ItemPerPage
        {
            readonly get => _itemPerPage;
            set
            {
                if (value != _itemPerPage && value > 0)
                {
                    _itemPerPage = value;
                    FixPage(true);
                }
            }
        }

        public int PageCount { get; private set; }

        public List<T> Data { get; private set; }

        public Pagination(List<T> data) {
            _itemPerPage = 30;
            CurrentPage = 1;
            PageCount = 1;
            Data = data;
            FixPage(true);
        }

        public void PreviousPage() {
            if (CurrentPage - 1 > 0)
            {
                CurrentPage--;
            }
        }

        public void NextPage() {
            if (CurrentPage + 1 <= PageCount)
            {
                CurrentPage++;
            }
        }

        public bool TryGetPaginationData(out int firstItemIndex, out int lastItemIndex) {
            firstItemIndex = (CurrentPage - 1) * ItemPerPage;
            lastItemIndex = ItemCount - firstItemIndex > ItemPerPage ? firstItemIndex + ItemPerPage : ItemCount;
            return firstItemIndex < ItemCount;
        }

        /// <summary>
        /// Tries to fix current page, updates page count
        /// </summary>
        /// <param name="reset">force reset</param>
        /// <returns><true> when current page reset was performed, regardless of reset argument</returns>
        public bool FixPage(bool reset) {
            PageCount = ItemCount > ItemPerPage ? (ItemCount / ItemPerPage) + ((ItemCount % ItemPerPage) > 0 ? 1 : 0) : 1;
            if (reset || !TryGetPaginationData(out int firstItemIndex, out int lastItemIndex))
            {
                CurrentPage = 1;
                return true;
            }
            return false;
        }
    }

}
