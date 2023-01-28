namespace ConsoleApp
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public class DataReader
    {
        IEnumerable<ImportedObject> ImportedObjects;

        public void ImportAndPrintData(string fileToImport) //nieużywany parametr  bool printData = true
        {
            ImportedObjects = new List<ImportedObject>(); //niepotrzebna inicjalizacja nowego obiektu listy - wystarczy lista

            try //brakowało bloku try-catch
            {
                var importedLines = new List<string>();
                //proponuję użycie using - ma w sobie interfejs IDisposable
                using (StreamReader sr  = new StreamReader(fileToImport))
                {
                    string sLine;
                    while ((sLine = sr.ReadLine()) != null)
                    {
                        importedLines.Add(sLine);
                    }
                }               

                for (int i = 0; i < importedLines.Count; i++) //błąd w petli for przy znaku < była <= - indeks z poza zakresu
                {
                    var importedLine = importedLines[i];
                    if (string.IsNullOrWhiteSpace(importedLine))
                        continue;
                    var values = importedLine.Split(';');
                    ((List<ImportedObject>)ImportedObjects).Add(new ImportedObject().CreateFromData(values)); //osobna akcja do przypisywania danych z tablity string - uniknięcie IndexOutOfRangeException
                }

                // clear and correct imported data
                foreach (var importedObject in ImportedObjects)
                {
                    importedObject.Type = importedObject.Type?.Trim().Replace(" ", "").Replace(Environment.NewLine, "").ToUpper(); //brakowało sprawdzenia czy properties nie sa null
                    importedObject.Name = importedObject.Name?.Trim().Replace(" ", "").Replace(Environment.NewLine, "");
                    importedObject.Schema = importedObject.Schema?.Trim().Replace(" ", "").Replace(Environment.NewLine, "");
                    importedObject.ParentName = importedObject.ParentName?.Trim().Replace(" ", "").Replace(Environment.NewLine, "");
                    importedObject.ParentType = importedObject.ParentType?.Trim().Replace(" ", "").Replace(Environment.NewLine, "");
                }

                // assign number of children
                for (int i = 0; i < ImportedObjects.Count(); i++)
                {
                    var importedObject = ImportedObjects.ToArray()[i];
                    foreach (var impObj in ImportedObjects)
                    {
                        if (impObj.ParentType == importedObject.Type)
                        {
                            if (impObj.ParentName == importedObject.Name)
                            {
                                importedObject.NumberOfChildren = 1 + importedObject.NumberOfChildren;
                            }
                        }
                    }
                }

                foreach (var database in ImportedObjects)
                {
                    if (database.Type == "DATABASE")
                    {
                        Console.WriteLine($"Database '{database.Name}' ({database.NumberOfChildren} tables)");

                        // print all database's tables
                        foreach (var table in ImportedObjects)
                        {
                            if (table.ParentType?.ToUpper() == database.Type) //sprawdzenie nulla
                            {
                                if (table.ParentName == database.Name)
                                {
                                    Console.WriteLine($"\tTable '{table.Schema}.{table.Name}' ({table.NumberOfChildren} columns)");

                                    // print all table's columns
                                    foreach (var column in ImportedObjects)
                                    {
                                        if (column.ParentType?.ToUpper() == table.Type) //sprawdzenie nulla
                                        {
                                            if (column.ParentName == table.Name)
                                            {
                                                Console.WriteLine($"\t\tColumn '{column.Name}' with {column.DataType} data type {(column.IsNullable == "1" ? "accepts nulls" : "with no nulls")}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                Console.ReadLine();
            }
            catch (Exception ex)
            {
                //tutaj komunikat co się stało
                Console.WriteLine(ex.Message);
                throw new Exception(ex.Message);
            }
        }

    }

    class ImportedObject : ImportedObjectBaseClass
    {
        //klasa bazowa zawiera już pole Name - nie jest potrzebne deklarowanie kolejnego takiego pola
        //brakuje mi getterów i setterów przy pozostałych property
        public string Schema { get; set; }
        public string ParentName { get; set; }
        public string ParentType { get; set; }
        public string DataType { get; set; }
        public string IsNullable { get; set; }
        public double NumberOfChildren { get; set; }

        //akcja mapowania elementów tablicy
         Action<ImportedObject, string>[] PropertyMappings =
        {
                 (a, s) => a.Type = s,
                 (a, s) => a.Name = s,
                 (a, s) => a.Schema = s,
                 (a, s) => a.ParentName = s,
                 (a, s) => a.ParentType = s,
                 (a, s) => a.DataType = s,
                 (a, s) => a.IsNullable = s
         };

        public ImportedObject CreateFromData(string[] data)
        {
            var result = new ImportedObject();
            for (var i = 0; i < data.Length; i++)
            {
                 PropertyMappings[i](result, data[i]);
            }
            return result;
        }
    }

    class ImportedObjectBaseClass
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
}
