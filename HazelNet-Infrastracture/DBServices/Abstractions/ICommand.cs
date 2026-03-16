namespace HazelNet_Infrastracture.DBServices.Abstractions;

public interface ICommand : IBaseCommand
{
}

public interface ICommand<T> : IBaseCommand
{
}

public interface IBaseCommand
{
}