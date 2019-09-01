﻿// PipelinePluginForm.cs
// (c) 2011-2019, Charles Lechasseur
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using PathCopyCopy.Settings.Core;
using PathCopyCopy.Settings.Core.Plugins;
using PathCopyCopy.Settings.UI.Forms;

namespace PathCopyCopy.Settings.UI.Utils
{
    /// <summary>
    /// Class that is responsible for editing new and existing
    /// pipeline plugins. Will show a form as appropriate.
    /// </summary>
    public sealed class PipelinePluginEditor
    {
        /// Owner of any form we create.
        private IWin32Window owner;

        /// Object to access user settings.
        private UserSettings settings;

        /// Plugin info for the plugin we're editing.
        private PipelinePluginInfo pluginInfo;

        /// Pipeline of the plugin info, if we have one.
        private Pipeline pipeline;

        /// <summary>
        /// Edits a new or existing pipeline plugin.
        /// </summary>
        /// <param name="owner">Owner to use for new forms. Can be <c>null</c>.</param>
        /// <param name="settings">Object to access user settings. Can be
        /// <c>null</c>, in which case a new one will be created.</param>
        /// <param name="oldInfo">Info about pipeline plugin to edit. If <c>null</c>,
        /// a new pipeline plugin will be edited.</param>
        /// <returns>Info about pipeline plugin edited. Will be <c>null</c> if the
        /// user cancelled the editing.</returns>
        public static PipelinePluginInfo EditPlugin(IWin32Window owner,
            UserSettings settings, PipelinePluginInfo oldInfo)
        {
            PipelinePluginEditor editor = new PipelinePluginEditor(owner, settings, oldInfo);
            return editor.Edit();
        }

        /// <summary>
        /// Private constructor called via
        /// <see cref="PipelinePluginEditor.EditPlugin(IWin32Window, UserSettings, PipelinePluginInfo)"/>
        /// </summary>
        /// <param name="owner">Owner to use for new forms. Can be <c>null</c>.</param>
        /// <param name="settings">Object to access user settings. Can be
        /// <c>null</c>, in which case a new one will be created.</param>
        /// <param name="oldInfo">Info about pipeline plugin to edit. If <c>null</c>,
        /// a new pipeline plugin will be edited.</param>
        private PipelinePluginEditor(IWin32Window owner, UserSettings settings,
            PipelinePluginInfo oldInfo)
        {
            // Save owner for any form we create.
            this.owner = owner;

            // Save settings or create new object.
            this.settings = settings ?? new UserSettings();

            // Save old plugin info if we have one.
            pluginInfo = oldInfo;

            // If a plugin info was specified, decode its pipeline immediately.
            // We want pipeline exceptions to propagate out *before* we show a form.
            if (pluginInfo != null) {
                pipeline = PipelineDecoder.DecodePipeline(pluginInfo.EncodedElements);
            }
        }

        /// <summary>
        /// Edits our pipeline plugin.
        /// </summary>
        /// <returns>Info about pipeline plugin edited. Will be <c>null</c> if the
        /// user cancelled the editing.</returns>
        private PipelinePluginInfo Edit()
        {
            if (pipeline == null || IsPipelineSimple(pipeline)) {
                using (PipelinePluginForm editForm = new PipelinePluginForm()) {
                    return editForm.EditPlugin(owner, settings, pluginInfo);
                }
            } else {
                throw new PipelinePluginEditorException("Pipeline plugin is too complex to edit");
            }
        }

        /// <summary>
        /// Determines if the given pipeline is "simple" - e.g., can be
        /// edited with the simple form instead of the advanced form.
        /// </summary>
        /// <param name="pipeline">Pipeline to validate. Cannot be <c>null</c>.</param>
        /// <returns><c>true</c> if the pipeline can be edited with the
        /// simple form.</returns>
        private static bool IsPipelineSimple(Pipeline pipeline)
        {
            Debug.Assert(pipeline != null);

            bool simple = false;
            if (pipeline.Elements.Count > 0) {
                // Pipeline must start with an ApplyPlugin element.
                if (pipeline.Elements.First() is ApplyPluginPipelineElement) {
                    // All other elements must be of different types and must
                    // not be ApplyPlugin elements.
                    if (pipeline.Elements.Distinct(new PipelineElementEqualityComparerByClassType()).Count() == pipeline.Elements.Count) {
                        // This is a simple pipeline.
                        simple = true;
                    }
                }
            }

            return simple;
        }

        /// <summary>
        /// Custom equality comparer for <see cref="PipelineElement"/>
        /// that compares using class type only.
        /// </summary>
        private sealed class PipelineElementEqualityComparerByClassType : IEqualityComparer<PipelineElement>
        {
            /// <summary>
            /// Determines if two pipeline elements are equal. In our case,
            /// we check their class types.
            /// </summary>
            /// <param name="x">First element to compare.</param>
            /// <param name="y">Second element to compare.</param>
            /// <returns><c>true</c> if <paramref name="x"/> is the same
            /// class type as <paramref name="y"/>.</returns>
            public bool Equals(PipelineElement x, PipelineElement y)
            {
                Debug.Assert(x != null);
                Debug.Assert(y != null);

                return x.GetType().Equals(y.GetType());
            }

            /// <summary>
            /// Returns a hash code for a pipeline element.
            /// </summary>
            /// <param name="obj">Element to get hash code for.</param>
            /// <returns>Hash code.</returns>
            public int GetHashCode(PipelineElement obj)
            {
                Debug.Assert(obj != null);

                return obj.GetType().GetHashCode();
            }
        }
    }

    /// <summary>
    /// Exception thrown when something goes wrong when editing a pipeline plugin.
    /// </summary>
    public class PipelinePluginEditorException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public PipelinePluginEditorException()
            : base()
        {
        }

        /// <summary>
        /// Constructor with exception message.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public PipelinePluginEditorException(string message)
            : base(message)
        {
        }
    }
}
