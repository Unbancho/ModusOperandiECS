using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using ModusOperandi.ECS.Archetypes;
using ModusOperandi.ECS.Entities;
using ModusOperandi.ECS.Scenes;
using ModusOperandi.Rendering;

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
    public interface IUpdateSystem
    {
        void Execute(float deltaTime = 0, bool parallel = true, SpriteBatch spriteBatch = null);
    }

    [PublicAPI]
    public interface IDrawSystem
    {
        void Draw(SpriteBatch spriteBatch);
    }

    [PublicAPI]
    public abstract class System
    {
        public Archetype Archetype { get; }

        protected System(Archetype archetype)
        {
            Archetype = archetype;
            SceneManager.ArchetypeEntityDictionary[Archetype] = SceneManager.Query(Archetype).ToArray();
        }

        protected ref TC Get<TC>(uint entity) where TC : unmanaged
        {
            return ref SceneManager.GetComponentManager<TC>().GetComponent(entity);
        }
    }
    
    public abstract class UpdateSystem : System, IUpdateSystem
    {
        protected abstract void ActOnComponents(uint entity, float deltaTime);

        protected UpdateSystem(Archetype archetype) : base(archetype)
        {
        }
        
        public virtual void Execute(float deltaTime, bool parallel = true, SpriteBatch spriteBatch = null)
        {
            if (parallel)
                ParallelExecute(deltaTime);
            else
            {
                var entities = SceneManager.ArchetypeEntityDictionary[Archetype];
                for (var i = 0; i < entities.Length; i++)
                    ActOnComponents(entities[i].ID, deltaTime);
            }

        }
        
        private void ParallelExecute(float deltaTime)
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
                            ActOnComponents(entities[i].ID, deltaTime);
                        }
                    });
            }

            Task.WaitAll(tasks);
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