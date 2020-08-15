using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using ModusOperandi.ECS.Archetypes;
using ModusOperandi.ECS.Entities;
using ModusOperandi.ECS.Scenes;

namespace ModusOperandi.ECS.Systems
{
    [PublicAPI]
    public class SystemAttribute : Attribute
    {
        public SystemAttribute(params Type[] systemDependencies)
        {
            SystemDependencies = systemDependencies;
        }

        public Type[] SystemDependencies { get; }
    }

    [PublicAPI]
    public interface ISystem
    {
        void Execute(float deltaTime = 0, bool parallel = true, Scene.Context context = default);
    }

    [PublicAPI]
    public abstract class System : ISystem
    {
        public Archetype Archetype { get; }

        protected System(Archetype archetype)
        {
            Archetype = archetype;
            SceneManager.ArchetypeEntityDictionary[Archetype] = SceneManager.Query(Archetype).ToArray();
        }

        public virtual void Execute(float deltaTime, bool parallel = true, Scene.Context context = default)
        {
            if (parallel)
                ParallelExecute(deltaTime, context);
            else
            {
                var entities = SceneManager.ArchetypeEntityDictionary[Archetype];
                for (var i = 0; i < entities.Length; i++)
                    ActOnComponents(entities[i].ID, (uint) i, deltaTime, context);
            }

        }
        
        private void ParallelExecute(float deltaTime, Scene.Context context)
        {
            var entities = SceneManager.ArchetypeEntityDictionary[Archetype];
            
            var degreeOfParallelism = Environment.ProcessorCount;

            var tasks = new Task[degreeOfParallelism];

            for (var taskNumber = 0; taskNumber < degreeOfParallelism; taskNumber++)
            {
                var taskNumberCopy = taskNumber;

                tasks[taskNumber] = Task.Factory.StartNew(
                    () =>
                    {
                        var max = entities.Length * (taskNumberCopy + 1) / degreeOfParallelism;
                        for (var i = entities.Length * taskNumberCopy / degreeOfParallelism;
                            i < max;
                            i++)
                        {
                            ActOnComponents(entities[i].ID, (uint) i, deltaTime, context);
                        }
                    });
            }

            Task.WaitAll(tasks);
        }

        protected abstract void ActOnComponents(uint entity, uint index, float deltaTime, Scene.Context context);

        protected ref TC Get<TC>(uint entity) where TC : unmanaged
        {
            return ref SceneManager.GetComponentManager<TC>().GetComponent(entity);
        }
    }

    [PublicAPI]
    public abstract class SystemGroupAttribute : Attribute
    {
    }

    [PublicAPI]
    public class UpdateSystemAttribute : SystemGroupAttribute
    {
    }

    [PublicAPI]
    public class DrawSystemAttribute : SystemGroupAttribute
    {
    }
}