using System;
namespace capstone_mongo.Services
{
    public class SessionService
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public SessionService(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public string ModuleCode
        {
            get
            {
                return httpContextAccessor.HttpContext.Session.GetString("moduleCode");
            }
            set
            {
                httpContextAccessor.HttpContext.Session.SetString("moduleCode", value);
            }
        }
    }


}

