namespace smswebapp.Models;
public class Student{
    public int Id {get; set;}
    public string Name {get; set;}
    public string Address {get; set;}

    public Student(string name)
        {
            Name = name; // Initialize the property
        }
}
