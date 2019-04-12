using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BrokerFacade.Test
{
    public class TestUtil
    {
        public static Uri LocalDockerUri()
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            return isWindows ? new Uri("npipe://./pipe/docker_engine") : new Uri("unix:/var/run/docker.sock");
        }


        public static void RemoveContainer(DockerClient client, string containerId)
        {
            var removal = new ContainerRemoveParameters() { Force = true };
            client.Containers.RemoveContainerAsync(containerId, removal).Wait();
        }

        public static string StartContainer(DockerClient client, string image, List<string> envs, Dictionary<string, string> ports, List<string> cmds = null)
        {
            if (cmds == null)
            {
                cmds = new List<string>();
            }
            var config = new Config
            {
                Image = image,
                Env = envs,
                ExposedPorts = new Dictionary<string, EmptyStruct>()
                {
                },
                Cmd = cmds

            };
            foreach (KeyValuePair<string, string> port in ports)
            {
                config.ExposedPorts.Add(port.Key, new EmptyStruct());
            }

            var createContainerParams = new CreateContainerParameters(config)
            {
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>()
                },
            };
            foreach (KeyValuePair<string, string> port in ports)
            {
                createContainerParams.HostConfig.PortBindings.Add(port.Key, new List<PortBinding>()
            {
                new PortBinding()
                {
                    HostIP = "",
                    HostPort = port.Value
                }
            });
            }
            CreateContainerResponse response = client.Containers.CreateContainerAsync(createContainerParams).Result;
            ContainerStartParameters startParams = new ContainerStartParameters();
            client.Containers.StartContainerAsync(response.ID, startParams).Wait();
            return response.ID;
        }
    }
}
