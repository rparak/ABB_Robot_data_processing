MODULE Module1
    VAR jointtarget J_Orientation_Target{5};
    VAR num in_position := 0;
    VAR num main_state := 0;
    
    PROC main()
        TEST main_state
            CASE 0:
                in_position := 0;
                main_state := 1;
                
            CASE 1:
                FOR i FROM 1 TO 5 DO
                    MoveAbsJ J_Orientation_Target{i} \NoEOffs,v100,fine,tool0\WObj:=wobj0;
                ENDFOR
                main_state := 2;
                
            CASE 2:
                in_position := 1;
                main_state := 3;
        ENDTEST
    ENDPROC
ENDMODULE