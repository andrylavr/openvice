﻿using OpenVice.Data;
using OpenVice.Entities;
using System.Collections.Generic;
using System.Threading;
using OpenTK;

namespace OpenVice.Managers {

	/// <summary>
	/// Class that handles all city meshes<para/>
	/// Класс, содержащий все городские меши
	/// </summary>
	public static class StaticManager {

		/// <summary>
		/// All of the static meshes on the map<para/>
		/// Все статические меши на карте
		/// </summary>
		public static List<StaticProxy> Statics = new List<StaticProxy>();

		/// <summary>
		/// Index of huge first island LOD<para/>
		/// Индекс огромного LOD-меша первого острова
		/// </summary>
		const int BeachLODIndex = 2634;

		/// <summary>
		/// Index of huge second island LOD<para/>
		/// Индекс огромного LOD-меша второго острова
		/// </summary>
		const int MainlandLODIndex = 2600;

		/// <summary>
		/// Background thread for mesh culling<para/>
		/// Фоновый поток для обработки близлежащих мешей
		/// </summary>
		static Thread thread;

		/// <summary>
		/// Transform placements to proxys<para/>
		/// Преобразование расстановок в прокси-объекты
		/// </summary>
		public static void Init() {

			// Firstly - normal objects
			// Сначала - обычные объекты
			foreach (ItemPlacement p in ObjectManager.Placements) {
				ItemDefinition ide = ObjectManager.Definitions[p.ID];
				if (!ide.ModelName.StartsWith("lod")) {
					// Creating non-lod fields
					// Заполнение обычных полей
					StaticProxy sp = new StaticProxy();
					sp.MainMesh = new StaticProxy.Group() {
						Placement = p,
						Definition = ide,
						Range = ide.DrawDistance[0],
						Coords = new Graphics.Transform() {
							Position = p.Position,
							Scale = p.Scale,
							Angles = p.Angle,
						},
						Timed = ide.IsTimed,
						HourOn = ide.TimeOn,
						HourOff = ide.TimeOff
					};
					sp.State = StaticProxy.VisState.Hidden;
					Statics.Add(sp);

					// Catch huge island LODs
					// Выборка огромных LOD-мешей
					if (p.ID == BeachLODIndex) {
						StaticProxy.BeachLod = sp;
					}else if(p.ID == MainlandLODIndex) {
						StaticProxy.MainlandLod = sp;
					}
				}
			}

			// Secondly - LOD objects
			// Потом - LOD-объекты
			foreach (ItemPlacement p in ObjectManager.Placements) {
				ItemDefinition ide = ObjectManager.Definitions[p.ID];
				if (ide.ModelName.StartsWith("lod")) {

					// Search for a parent
					// Поиск родителя
					StaticProxy sp = null;
					ItemDefinition pide = null;
					foreach (StaticProxy spp in Statics) {
						if (!spp.LodAssigned) {
							pide = ObjectManager.Definitions[spp.MainMesh.Definition.ID];
							if (pide.ModelName.Length>3) {
								if (ide.ModelName == "lod" + pide.ModelName.Substring(3)) {
									sp = spp;
									break;
								}
							}
						}
					}

					if (sp == null) {
						// Owner is not found - it's a simple object
						// Родитель не нашёлся - это простой объект
						sp = new StaticProxy();
						sp.MainMesh = new StaticProxy.Group() {
							Definition = ide,
							Placement = p,
							Range = ide.DrawDistance[0],
							Coords = new Graphics.Transform() {
								Position = p.Position,
								Scale = p.Scale,
								Angles = p.Angle,
							},
							Timed = ide.IsTimed,
							HourOn = ide.TimeOn,
							HourOff = ide.TimeOff
						};
						sp.State = StaticProxy.VisState.Hidden;
						sp.LodAssigned = true;
						Statics.Add(sp);
					}else{
						// Creating lod fields
						// Заполнение низкополигональных полей
						sp.LODMesh = new StaticProxy.Group() {
							Definition = ide,
							Placement = p,
							Range = ide.DrawDistance[0],
							Coords = new Graphics.Transform() {
								Position = p.Position,
								Scale = p.Scale,
								Angles = p.Angle,
							},
							Timed = ide.IsTimed,
							HourOn = ide.TimeOn,
							HourOff = ide.TimeOff
						};
						sp.LodAssigned = true;
					}
				}
			}
			Dev.Console.Log("[StaticManager] Generated "+Statics.Count+" proxies");

			// Starting the culler thread
			// Запуск отсекающего потока
			thread = new Thread(CullingProcess);
			thread.IsBackground = true;
			thread.Priority = ThreadPriority.BelowNormal;
			thread.Start();
			Dev.Console.Log("[StaticManager] Culling thread started");
		}

		/// <summary>
		/// Render objects<para/>
		/// Отправка объектов на рендер
		/// </summary>
		public static void Render(float delta) {
			foreach (StaticProxy p in Statics) {
				p.Render(delta);
			}
		}

		/// <summary>
		/// Update objects<para/>
		/// Обновление логики объектов
		/// </summary>
		public static void Update(float delta) {
			foreach (StaticProxy p in Statics) {
				p.Update(delta);
			}
		}

		/// <summary>
		/// Background culling process<para/>
		/// Фоновый процесс отсечения
		/// </summary>
		static void CullingProcess() {
			//int cullCount = 0;
			while(true){
				foreach (StaticProxy p in Statics) {
					if (p == StaticProxy.MainlandLod || p == StaticProxy.BeachLod) {




					} else {
						// Check for active interior
						// Проверка на текущий интерьер
						if (p.MainMesh.Placement.InteriorID == CityManager.Interior || p.MainMesh.Placement.InteriorID == ItemPlacement.Interior.Everywhere) {
							// Check for mesh visibility
							// Проверка на видимость меша
							Vector2 cam = new Vector2(Camera.Position.X, Camera.Position.Z);
							Vector2 range = new Vector2(p.MainMesh.Coords.Position.X, p.MainMesh.Coords.Position.Z);
							p.State = StaticProxy.VisState.Hidden;

							float dist = (range - cam).LengthSquared;
							p.CollisionActive = dist <= PhysicsManager.VisibleRange * PhysicsManager.VisibleRange * 1.2f;

							if (p.MainMesh.Definition.Flags[ItemDefinition.DefinitionFlags.DisableDrawDistance] || dist <= p.MainMesh.Range * p.MainMesh.Range) {
								// Main mesh is visible
								// Основной меш виден
								p.State = StaticProxy.VisState.MeshVisible;
							} else {
								if (p.LODMesh != null) {
									range = new Vector2(p.LODMesh.Coords.Position.X, p.LODMesh.Coords.Position.Z);
									if (p.LODMesh.Definition.Flags[ItemDefinition.DefinitionFlags.DisableDrawDistance] || (range - cam).LengthSquared <= p.LODMesh.Range * p.LODMesh.Range) {
										// LOD is visible
										// LOD-меш виден
										p.State = StaticProxy.VisState.LodVisible;
									}
								}
							}
						} else {
							p.State = StaticProxy.VisState.Hidden;
							p.CollisionActive = false;
						}
					}
					Thread.Sleep(0);
				}
			}
		}

	}
}
