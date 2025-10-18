namespace SchoolSystem.Models.AdminSchool
{
    public class GetCreateClass
    {


        public bool IsBranche { get; set; }
        public List<BranchClass> Branches { get; set; }

    }

    public class BranchClass
    {
        public string idBranch { get; set; }
        public string nameBranch { get; set; }


    }
}