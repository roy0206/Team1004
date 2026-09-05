using System;
using System.Collections.Generic;

namespace Game.Spawner
{
    public sealed class DifficultyProfile
    {
        private readonly SectionProfile[] sections;

        public DifficultyProfile(IReadOnlyList<SectionProfile> sections)
        {
            if (sections == null || sections.Count == 0)
                throw new ArgumentException("Difficulty profile needs at least one section.", nameof(sections));

            this.sections = new SectionProfile[sections.Count];
            for (var i = 0; i < sections.Count; i++)
                this.sections[i] = sections[i] ?? throw new ArgumentException("Section " + i + " is null.", nameof(sections));
        }

        public int SectionCount => sections.Length;

        public SectionProfile GetSection(int sectionIndex)
        {
            return sections[Math.Clamp(sectionIndex, 0, sections.Length - 1)];
        }

        public DifficultySample Sample(int sectionIndex, float progress)
        {
            return GetSection(sectionIndex).Sample(progress);
        }
    }
}
