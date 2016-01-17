﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace OpenVice.Graphics.Shaders {

	/// <summary>
	/// Shader for sky sphere<para/>
	/// Шейдер для сферы неба
	/// </summary>
	public class SkyShader : ShaderBase {
		/// <summary>
		/// Internal shader object<para/>
		/// Внутренний объект шейдера
		/// </summary>
		static SkyShader shader;

		/// <summary>
		/// Shader object access field<para/>
		/// Поле доступа к объекту шейдера
		/// </summary>
		public static SkyShader Shader {
			get {
				if (shader == null) {
					shader = new SkyShader();
					shader.CompileShader();
				}
				return shader;
			}
		}

		/// <summary>
		/// Seek associated uniforms<para/>
		/// Поиск ассоциированных униформов
		/// </summary>
		protected override void SeekUniforms() {
			GL.UseProgram(glprog);
			ProjectionMatrix = GL.GetUniformLocation(glprog, "projMatrix");
			ModelViewMatrix = GL.GetUniformLocation(glprog, "modelMatrix");
			TopColor = GL.GetUniformLocation(glprog, "topColor");
			BottomColor = GL.GetUniformLocation(glprog, "bottomColor");
			GL.UseProgram(0);
		}

		/// <summary>
		/// Get fragment shader code<para/>
		/// Получение кода фрагментного шейдера
		/// </summary>
		protected override string GetFragmentCode() {
			return fragmentProg;
		}

		/// <summary>
		/// Get vertex shader code<para/>
		/// Получение кода вершинного шейдера
		/// </summary>
		protected override string GetVertexCode() {
			return vertexProg;
		}

		/// <summary>
		/// Camera projection matrix<para/>
		/// Матрица проекции камеры
		/// </summary>
		public static int ProjectionMatrix { get; private set; }

		/// <summary>
		/// Camera position matrix<para/>
		/// Матрица расположения камеры
		/// </summary>
		public static int ModelViewMatrix { get; private set; }

		/// <summary>
		/// Top color<para/>
		/// Цвет верхней части
		/// </summary>
		public static int TopColor { get; set; }

		/// <summary>
		/// Bottom color<para/>
		/// Цвет нижней части
		/// </summary>
		public static int BottomColor { get; set; }

		/// <summary>
		/// Vertex program for this shader<para/>
		/// Вершинная программа для этого шейдера
		/// </summary>
		static string vertexProg = @"
			// Basic uniforms list
			// Список юниформов
			uniform mat4 projMatrix;
			uniform mat4 modelMatrix;
			uniform vec3 topColor;
			uniform vec3 bottomColor;
			
			// Fragment color
			// Цвет фрагмента
			varying vec3 fc;
			
			// Processing vertex
			// Обработка вершины
			void main() {
				mat4 completeMat = projMatrix * modelMatrix;
				fc = mix(bottomColor, topColor, clamp(gl_Vertex.y, 0.0, 1.0));
				gl_Position = completeMat * gl_Vertex;
			}
		";

		/// <summary>
		/// Fragment program for this shader<para/>
		/// Фрагментная программа для этого шейдера
		/// </summary>
		static string fragmentProg = @"
			// Fragment color
			// Цвет фрагмента
			varying vec3 fc;

			// Processing fragment
			// Обработка фрагмента
			void main() {
				gl_FragColor = vec4(fc, 1.0);
			}
		";

	}
}
