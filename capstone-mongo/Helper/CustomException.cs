using System;
using capstone_mongo.Helper;
using capstone_mongo.Models;

namespace capstone_mongo.Helper
{
    public class CustomException : Exception
    {
        public CustomException()
        {
        }

        public CustomException(string message)
            : base(message)
        {
        }

        public CustomException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class InvalidFileException : CustomException
    {
        public InvalidFileException()
            : base()
        {
        }

        public InvalidFileException(string message)
            : base(message)
        {
        }

        public InvalidFileException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class AccessDenied : CustomException
    {
        public AccessDenied(string message) : base(message)
        {
        }
    }
    public class DuplicateModuleException : Exception
    {
        public string ModuleCode { get; }

        public DuplicateModuleException(string moduleCode)
            : base(CreateMessage(moduleCode))
        {
            ModuleCode = moduleCode;
        }

        private static string CreateMessage(string moduleCode)
        {
            return $"Module with code '{moduleCode}' already exists.";
        }
    }
    public class ServiceException : Exception
    {
        public string AlertClass { get; }

        public ServiceException(string message, string alertClass) : base(message)
        {
            AlertClass = alertClass;
        }
    }
}