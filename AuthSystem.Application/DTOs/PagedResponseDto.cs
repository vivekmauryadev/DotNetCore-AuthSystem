using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthSystem.Application.DTOs
{
    public class PagedResponseDto<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
