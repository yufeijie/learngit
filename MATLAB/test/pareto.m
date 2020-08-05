function dataPareto = pareto(data)
n = size(data, 1);
dataPareto = zeros();
m = 0;
for i = 1 : n
    p = 1;
    for j = 1:m
        if data(i, 1) <= dataPareto(j, 1) && data(i, 2) <= dataPareto(j, 2) && data(i, 3) >= dataPareto(j, 3)
            for k = j:m-1
                dataPareto(k, 1) = dataPareto(k+1, 1);
                dataPareto(k, 2) = dataPareto(k+1, 2);
                dataPareto(k, 3) = dataPareto(k+1, 3);
            end
            j = j-1;
            m = m-1;
        else
            if data(i, 1) >= dataPareto(j, 1) && data(i, 2) >= dataPareto(j, 2) && data(i, 3) <= dataPareto(j, 3)
                p = 0;
                break;
            end
        end
    end
    if p == 1
        m = m+1;
        for k = 1 : size(data, 2)
            dataPareto(m, k) = data(i, k);
        end
    end
end
dataPareto = dataPareto(1:m,:);
end


