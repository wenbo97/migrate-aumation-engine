using Microsoft.SemanticKernel;

namespace Automation.KernelContainerProvider;

public interface IKernelProvider
{
    Kernel GetKernel();
}