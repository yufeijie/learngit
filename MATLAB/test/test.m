function Z = test(design, X, Y)
m = size(X, 1);
n = size(X, 2);
Z = zeros(m, n);
for i = 1:m
    for j = 1:n
        Z(i, j) = 99;
    end
end
for i = 1:m
    for j = 1:n
        for k = 1:size(design, 1)
            if design(k, 2) == X(i,j) && design(k, 1) == Y(i,j) && design(k, 3) > Z(i,j)
                Z(i, j) = design(k, 3);
            end
        end
    end
end
end