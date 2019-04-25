Imports Microsoft.SolverFoundation.Common
Imports Microsoft.SolverFoundation.Services
Imports Microsoft.SolverFoundation.Solvers
'****************************************************************************************************************
Public Class Optimization
    Dim HW5RPMModel As New SimplexSolver
    Dim dvKey As String
    Dim dvindex As Integer
    Dim coefficient As Single
    Dim constraintKey As String
    Dim ConstraintIndex As Integer
    Dim objKey As String = "Objective Function"

    Dim objIndex As Integer = 1
    Public optimalObj As Single
    Public dvValue(Period.PeriodList.Count - 1, Process.ProcessList.Count - 1) As Single
    '****************************************************************************************************************
    Public Sub RPMBuildModel()

        'Decision Variables
        For Each myEmployee As Employee In Employee.EmployeeList
            For Each myShift As Shift In Shift.ShiftList
                dvKey = myEmployee.WageRate & "_" & myShift.ShiftLength
                HW5RPMModel.AddVariable(dvKey, dvindex)
                HW5RPMModel.SetBounds(dvindex, 1, Rational.PositiveInfinity)
            Next
        Next
        '************************************************************************************************************
        'Constraints
        '
        'Capacity Constraints
        For Each myEmployee As Employee In Employee.EmployeeList
            constraintKey = "Capacity Constraint" & "_" & myEmployee.WageRate
            HW5RPMModel.AddRow(constraintKey, ConstraintIndex)


            For Each myDemand As Demand In Demand.DemandList
                ConstraintIndex = HW5RPMModel.GetIndexFromKey(constraintKey)
                coefficient = 1
                HW5RPMModel.SetCoefficient(ConstraintIndex, ConstraintIndex, coefficient)
            Next
            HW5RPMModel.SetBounds(ConstraintIndex, 0, coefficient)
        Next
        '***************************************************************************************
        For Each myShift As Shift In Shift.ShiftList
            constraintKey = "Requirement Constraint" & "_" & myShift.ShiftName
            HW5RPMModel.AddRow(constraintKey, ConstraintIndex)
            HW5RPMModel.SetBounds(ConstraintIndex, 0, Rational.PositiveInfinity)

            For Each myEmployee As Employee In Employee.EmployeeList
                dvindex = HW5RPMModel.GetIndexFromKey(dvKey)
                coefficient = 1
                HW5RPMModel.SetCoefficient(ConstraintIndex, ConstraintIndex, coefficient)
            Next
            HW5RPMModel.SetBounds(ConstraintIndex, 1, coefficient)
        Next
        '************************************************************************************************************
        '*************************************************************************************************************
        'ObjectiveFunction
        HW5RPMModel.AddRow(objKey, objIndex)
        For Each myEmp As Employee In Employee.EmployeeList
            For Each myShift As Shift In Shift.ShiftList
                objIndex = HW5RPMModel.GetIndexFromKey(objKey)
                'An alternate way to get the coefficents
                'coefficient = effect.Effect(emp.ActivityList.IndexOf(activity))
                If myEmp.EmployeeName = "Employee 1" Then coefficient = myShift.PeriodOverlap(0)
                If myEmp.EmployeeName = "Employee 2" Then coefficient = myShift.PeriodOverlap(1)
                If myEmp.EmployeeName = "Employee 3" Then coefficient = myShift.PeriodOverlap(2)
                If myEmp.EmployeeName = "Employee 4" Then coefficient = myShift.PeriodOverlap(3)

                HW5RPMModel.SetCoefficient(objIndex, dvindex, coefficient)

            Next
        Next
        HW5RPMModel.AddGoal(objIndex, 1, False)
    End Sub
    '****************************************************************************************************************
    Public Sub RPMRunModel()
        '----------------------------------------------------------------------------------------------------------
        'RDB:  Solve the optimization

        Dim mySolverParms As New SimplexSolverParams
        mySolverParms.MixedIntegerGapTolerance = 1              'RDB: For IP only - 1 percent gap tolerance between upper and lower bounds of objective function
        mySolverParms.VariableFeasibilityTolerance = 0.00001    'RDB: For IP only - required closeness to a whole number of each variable
        mySolverParms.MaxPivotCount = 1000                     'RDB: Number of iterations.  Increase as necessary
        HW5RPMModel.Solve(mySolverParms)

        'RDB:    We check to see if we got an answer
        If HW5RPMModel.Result = LinearResult.UnboundedPrimal Then
            MessageBox.Show("Solution is unbounded")
            Exit Sub
        ElseIf HW5RPMModel.Result = LinearResult.InfeasiblePrimal Then
            MessageBox.Show("Decision model is infeasible")
            Exit Sub
        Else
            ShowAnswer()
        End If
    End Sub
    '***************************************************************************************************************
    Public Sub ShowAnswer()
        '----------------------------------------------------------------------------------------------------------
        'RDB: Now we display the optimal values of the variables and objective function
        optimalObj = CSng(HW5RPMModel.GetValue(objIndex).ToDouble)

        'RDB: We transfer the values of the decision variables to an array 
        Dim rowIndex As Integer = 0
        Dim columnIndex As Integer = 0
        '
        For Each myEmployee As Employee In Employee.EmployeeList
            rowIndex = Employee.EmployeeList.IndexOf(myEmployee)
            For Each myShift As Shift In Shift.ShiftList
                columnIndex = Shift.ShiftList.IndexOf(myShift)
                dvKey = myEmployee.EmployeeName & "_" & myShift.ShiftName

                dvindex = HW5RPMModel.GetIndexFromKey(myEmployee.EmployeeName & "_" & myShift.ShiftName)
                dvValue(rowIndex, columnIndex) = CSng(HW5RPMModel.GetValue(dvindex).ToDouble)
            Next
        Next
        '************************************************************************************
        Solution.RPMTable.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
        '
        'RDB: We enter the column headings into the table
        For column As Integer = 1 To Solution.RPMTable.ColumnCount - 1
            Dim myLabel As New Label
            myLabel.Text = "Activity " & CStr(column)
            Solution.RPMTable.Controls.Add(myLabel)
            myLabel.Visible = True
            myLabel.TextAlign = ContentAlignment.MiddleCenter
            Solution.RPMTable.SetRow(myLabel, 0)
            Solution.RPMTable.SetColumn(myLabel, column)
            myLabel.Anchor = AnchorStyles.Bottom
            myLabel.Anchor = AnchorStyles.Top
            myLabel.Anchor = AnchorStyles.Left
            myLabel.Anchor = AnchorStyles.Right
        Next
        '
        'RDB: We enter the row headings into the table
        rowIndex = 0
        For Each proc As Process In Process.ProcessList
            Dim myLabel As New Label
            myLabel.Text = proc.ProcessTime
            myLabel.Visible = True
            myLabel.TextAlign = ContentAlignment.MiddleCenter
            Solution.RPMTable.SetRow(myLabel, rowIndex + 1)
            Solution.RPMTable.SetColumn(myLabel, 0)
            Solution.RPMTable.Dock = DockStyle.Fill
            Solution.RPMTable.Controls.Add(myLabel)
            myLabel.Anchor = AnchorStyles.Bottom
            myLabel.Anchor = AnchorStyles.Top
            myLabel.Anchor = AnchorStyles.Left
            myLabel.Anchor = AnchorStyles.Right
            rowIndex += 1
        Next

        For row As Integer = 1 To Solution.RPMTable.RowCount - 1
            For column As Integer = 1 To Solution.RPMTable.ColumnCount - 1
                Dim myLabel As New Label
                myLabel.Text = CStr(dvValue(row - 1, column - 1))
                myLabel.Visible = True
                myLabel.TextAlign = ContentAlignment.MiddleCenter
                Solution.RPMTable.SetRow(myLabel, row)
                Solution.RPMTable.SetColumn(myLabel, column)
                Solution.RPMTable.Dock = DockStyle.Fill
                Solution.RPMTable.Controls.Add(myLabel)
                myLabel.Anchor = AnchorStyles.Bottom
                myLabel.Anchor = AnchorStyles.Top
                myLabel.Anchor = AnchorStyles.Left
                myLabel.Anchor = AnchorStyles.Right
            Next
        Next
        Solution.Show()

    End Sub
End Class

